from datetime import timedelta
from odoo import fields, models, api
from odoo.exceptions import UserError, ValidationError
import logging

_logger = logging.getLogger(__name__)


class ProjectTask(models.Model):
    _name = "project.task"
    _inherit = ["project.task"]
    _order = "date_start asc, process_type asc, id desc"

    name = fields.Char(string="Наименование работы", required=True, default="Начало смены")

    employee_ids = fields.Many2many("hr.employee", string="Исполнители", default=False)

    worker_count = fields.Integer(string="Количество исполнителей", readonly=True)

    material_consumption_kg = fields.Float(
        string="Расход материала (кг)",
        help="Количество израсходованного материала в килограммах",
    )

    problem_description = fields.Text(string="Описание проблемы")

    people_fact = fields.Integer(string="Исполнителей по факту")

    auto_tracking = fields.Boolean(
        string="Авто-продолжение",
        default=True,
        help="Включить автоматическое взятие в работу при последовательном выполнении смен",
    )

    schedule_id = fields.Many2one(
        "task.schedule", string="Расписание", ondelete="cascade"
    )

    process_type = fields.Selection(
        [
            ("main", "Основной техпроцесс"),
            ("parallel", "Вспомогательные работы"),
        ],
        string="Тип процесса",
    )

    shift_number = fields.Selection(
        [(str(i), str(i)) for i in range(1, 9)], string="Номер смены"
    )

    maintenance_day = fields.Integer(string="День ТО оборудования")

    planning_series_datetime = fields.Datetime(
        string="Дата создания серии", readonly=True
    )

    date_start = fields.Datetime(
        string="Дата начала", required=True, default=fields.Datetime.now
    )

    shift = fields.Many2one(
        "custom_project.shift", string="Смена"
    )

    shift_code = fields.Selection(
        related="shift.code",
        string="Код смены",
        readonly=True
    )

    shift_color = fields.Integer(string="Цвет смены", compute="_compute_shift_color")

    production_cycle = fields.Char(string="Производственный цикл", compute="_compute_production_cycle")

    custom_task_type_id = fields.Many2one(
        "project.task.type", string="Тип работы"
    )

    actual_start_time = fields.Datetime(string="Фактическое время начала")
    actual_end_time = fields.Datetime(string="Фактическое время завершения")

    # Контроль качества
    quality_control_ids = fields.One2many(
        "quality.control", "task_id", string="Контроль качества"
    )

    quality_status = fields.Selection(
        [
            ("pending", "Ожидает контроля качества"),
            ("accepted", "Принято ОТК"),
            ("rejected", "Отклонено ОТК"),
        ],
        string="Статус контроля качества",
        compute="_compute_quality_status",
        store=True,
        help="Статус контроля качества от отдела технического контроля",
    )

    has_quality_control = fields.Boolean(
        string="Есть контроль качества",
        compute="_compute_has_quality_control",
        store=True,
        index=True,
    )

    @api.constrains(
        "project_id", "date_start", "process_type", "shift", "custom_task_type_id"
    )
    def _constrain_unique_task(self):
        for record in self:
            if not record.date_start:
                continue
            day_start = record.date_start.replace(
                hour=0, minute=0, second=0, microsecond=0
            )
            day_end = day_start.replace(
                hour=23, minute=59, second=59, microsecond=999999
            )
            domain = [
                ("project_id", "=", record.project_id.id),
                ("date_start", ">=", day_start),
                ("date_start", "<=", day_end),
                ("process_type", "=", record.process_type),
                ("shift", "=", record.shift.id),
                # ("custom_task_type_id", "=", record.custom_task_type_id.id),
                ("id", "!=", record.id),
            ]
            if self.search_count(domain) > 0:
                raise ValidationError(
                    "Работа с такими параметрами уже существует в эту смену!"
                )

    @api.model
    def _cron_auto_take_shift_tasks(self):
        """
        CRON задача для автоматического взятия в работу задач по сменам
        """
        _logger.info("Запуск автоматического взятия сменных задач")

        now_utc = fields.Datetime.now()
        today_start = now_utc.replace(hour=0, minute=0, second=0, microsecond=0)
        today_end = today_start.replace(hour=23, minute=59, second=59, microsecond=999999)

        today_tasks = self.search(
            [
                ("date_start", ">=", today_start),
                ("date_start", "<=", today_end),
                ("stage_id", "=", self.env.ref("custom_project.project_task_stage_planned").id),
                ("auto_tracking", "=", True),
            ]
        )

        tasks_auto_taken = []

        for task in today_tasks:
            if self._should_auto_take_task(task):
                # Находим предыдущую сменную задачу
                previous_shift_task = self._find_previous_shift_task(task)

                # Берем исполнителей из предыдущей задачи
                employee_ids = (
                    [(6, 0, previous_shift_task.employee_ids.ids)] if previous_shift_task else []
                )

                actual_time = fields.Datetime.now()

                task.write(
                    {
                        "employee_ids": employee_ids,
                        "stage_id": self.env.ref("custom_project.project_task_stage_in_progress").id,
                        "actual_start_time": actual_time,
                        "actual_end_time": actual_time,
                    }
                )
                tasks_auto_taken.append(task)

        _logger.info(
            f"Автоматически взяты в работу задачи: "
            f"{[f'ID: {task.id}' for task in tasks_auto_taken]}"
        )

    def _should_auto_take_task(self, task):
        """Проверяет, должна ли задача быть автоматически взята в работу"""
        previous_task = self._find_previous_shift_task(task)
        if not previous_task:
            return False

        completed_stage = self.env.ref("custom_project.project_task_stage_completed")
        return previous_task.stage_id == completed_stage

    def _find_previous_shift_task(self, current_task):
        """Находит задачу предыдущей смены"""
        if not current_task.date_start:
            return False

        # Для металлургии смена обычно 8 часов
        hours_per_shift = 8
        previous_time = current_task.date_start - timedelta(hours=hours_per_shift)

        return self.search(
            [
                ("project_id", "=", current_task.project_id.id),
                ("process_type", "=", current_task.process_type),
                ("custom_task_type_id", "=", current_task.custom_task_type_id.id),
                ("date_start", "<", current_task.date_start),
                ("id", "!=", current_task.id),
            ],
            limit=1,
            order="date_start desc",
        )

    @api.model
    def _adjust_date_start_for_shift(self, now, shift):
        """Корректировка date_start для смены"""
        if shift.code == 'night' and now.hour < shift.end_hour:
            return now.replace(hour=shift.start_hour, minute=0, second=0,
                               microsecond=0) - timedelta(days=1)
        return now.replace(hour=shift.start_hour, minute=0, second=0,
                           microsecond=0)

    @api.depends("quality_control_ids")
    def _compute_has_quality_control(self):
        for task in self:
            task.has_quality_control = bool(task.quality_control_ids)

    @api.depends("quality_control_ids.status")
    def _compute_quality_status(self):
        for task in self:
            if not task.quality_control_ids:
                task.quality_status = "pending"
                continue

            # Берем последний контроль качества
            last_control = task.quality_control_ids.sorted(key=lambda r: r.id, reverse=True)[0]
            task.quality_status = last_control.status

    @api.depends("shift_number", "date_start")
    def _compute_production_cycle(self):
        for record in self:
            if record.date_start:
                record.production_cycle = f"Цикл {record.shift_number} {record.date_start.year}"
            else:
                record.production_cycle = f"Цикл {record.shift_number}"

    @api.depends("shift")
    def _compute_shift_color(self):
        shift_colors = {"morning": 10, "day": 3, "night": 0}
        for record in self:
            record.shift_color = shift_colors.get(record.shift.code, 0)

    def action_delete_series(self):
        """Удаление серии через расписание"""
        if not self.schedule_id:
            raise UserError("Задача не связана с расписанием для удаления серии.")
        return self.schedule_id.action_delete_schedule_series()

    @api.onchange("stage_id")
    def _onchange_stage_id(self):
        """Обработка изменения stage_id в UI"""
        if self.stage_id:
            stage_in_progress = self.env.ref("custom_project.project_task_stage_in_progress", False)
            stage_completed = self.env.ref("custom_project.project_task_stage_completed", False)
            stage_planned = self.env.ref("custom_project.project_task_stage_planned", False)

            current_stage = self.stage_id
            now = fields.Datetime.now()

            if stage_in_progress and current_stage.id == stage_in_progress.id:
                if not self.actual_start_time:
                    self.actual_start_time = now
                if self.actual_end_time:
                    self.actual_end_time = False

            elif stage_completed and current_stage.id == stage_completed.id:
                if not self.actual_end_time:
                    self.actual_end_time = now
                if not self.actual_start_time:
                    self.actual_start_time = now

            elif stage_planned and current_stage.id == stage_planned.id:
                if self.actual_start_time:
                    self.actual_start_time = False
                if self.actual_end_time:
                    self.actual_end_time = False