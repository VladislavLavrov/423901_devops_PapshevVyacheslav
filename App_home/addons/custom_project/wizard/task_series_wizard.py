from datetime import timedelta
from odoo import models, fields, api, _
from odoo.exceptions import ValidationError
import logging

_logger = logging.getLogger(__name__)


class TaskSeriesWizard(models.TransientModel):
    _name = "project.task.series.wizard"
    _description = "Мастер создания производственного расписания"

    project_id = fields.Many2one("project.project", string="Цех/Участок", required=True)
    date_start = fields.Date(
        string="Дата начала планирования", required=True, default=fields.Date.context_today
    )

    process_type = fields.Selection(
        [
            ("main", "Основной техпроцесс"),
            ("parallel", "Вспомогательные работы"),
            ("both", "Все процессы"),
        ],
        string="Тип процессов",
        required=True,
        default="main",
    )

    start_shift_number = fields.Selection(
        [(str(i), str(i)) for i in range(1, 9)],
        string="Начать с смены №",
        required=True,
        default="1",
    )
    
    shift_count = fields.Selection(
        [(str(i), str(i)) for i in range(1, 9)],
        string="Количество смен",
        required=True,
        default="1",
    )
    
    equipment_maintenance_days = fields.Integer(
        string="Дней на ТО оборудования", default=0, required=True
    )

    estimated_tasks_count = fields.Integer(
        string="Примерное количество работ", compute="_compute_estimated_tasks"
    )
    
    estimated_end_date = fields.Date(
        string="Примерная дата окончания", compute="_compute_estimated_tasks"
    )
    
    date_start_display = fields.Date(
        string="Дата начала (отображение)", related="date_start", readonly=True
    )

    # Дополнительные параметры для металлургии
    product_type = fields.Selection(
        [
            ("steel", "Сталь"),
            ("cast_iron", "Чугун"),
            ("non_ferrous", "Цветные металлы"),
            ("alloy", "Сплавы"),
        ],
        string="Тип продукции",
        default="steel",
        required=True,
    )
    
    production_volume = fields.Float(
        string="Плановый объем (тонн)",
        default=100.0,
        help="Планируемый объем производства в тоннах"
    )
    
    priority_level = fields.Selection(
        [
            ("normal", "Обычный"),
            ("high", "Высокий"),
            ("urgent", "Срочный"),
        ],
        string="Приоритет производства",
        default="normal",
    )

    @api.depends("shift_count", "equipment_maintenance_days", "process_type", "date_start")
    def _compute_estimated_tasks(self):
        """Расчет количества работ и даты окончания"""
        for wizard in self:
            if not wizard.date_start:
                wizard.estimated_tasks_count = 0
                wizard.estimated_end_date = False
                continue

            # Расчет количества задач на основе типа процесса
            base_tasks_per_shift = 3  # Основные задачи в смену
            
            if wizard.process_type == "both":
                tasks_per_shift = base_tasks_per_shift + 2  # + вспомогательные работы
            elif wizard.process_type == "parallel":
                tasks_per_shift = 2  # Только вспомогательные работы
            else:
                tasks_per_shift = base_tasks_per_shift  # Только основные работы

            # Учет дней ТО
            maintenance_tasks = wizard.equipment_maintenance_days * 2  # 2 задачи в день на ТО
            
            total_tasks = int(wizard.shift_count) * tasks_per_shift + maintenance_tasks
            wizard.estimated_tasks_count = total_tasks

            # Расчет даты окончания
            days_per_shift_cycle = 1  # Каждая смена - 1 день
            total_calendar_days = int(wizard.shift_count) * days_per_shift_cycle
            
            # Добавляем дни на ТО оборудования
            total_calendar_days += wizard.equipment_maintenance_days
            
            wizard.estimated_end_date = wizard.date_start + timedelta(
                days=total_calendar_days
            )

    @api.constrains("equipment_maintenance_days")
    def _check_maintenance_days(self):
        for wizard in self:
            if wizard.equipment_maintenance_days < 0:
                raise ValidationError(_("Количество дней ТО не может быть отрицательным"))
            if wizard.equipment_maintenance_days > 30:
                raise ValidationError(_("Слишком много дней на ТО оборудования (максимум 30)"))

    @api.constrains("production_volume")
    def _check_production_volume(self):
        for wizard in self:
            if wizard.production_volume <= 0:
                raise ValidationError(_("Объем производства должен быть больше нуля"))
            if wizard.production_volume > 10000:
                raise ValidationError(_("Слишком большой объем производства (максимум 10,000 тонн)"))

    @api.constrains("date_start")
    def _check_date_start(self):
        for wizard in self:
            if wizard.date_start < fields.Date.context_today(self):
                raise ValidationError(_("Дата начала не может быть в прошлом"))

    # В models/production_planner_wizard.py (или вашем TaskSeriesWizard)

    def action_create_series(self):
        self.ensure_one()

        # Создаём расписание
        schedule = self.env["task.schedule"].create({
            "name": f"Расписание {self.project_id.name} от {self.date_start}",
            "project_id": self.project_id.id,
            "process_type": self.process_type,
            "start_shift": self.start_shift_number,
            "shift_count": self.shift_count,
            "equipment_maintenance_days": self.equipment_maintenance_days,
            "date_start": self.date_start,
        })

        # Делегируем генерацию задач
        try:
            schedule.action_generate_tasks()
        except Exception as e:
            raise ValidationError(_("Ошибка при генерации задач: %s") % str(e))

        # Возвращаем уведомление и открываем расписание
        return {
            "type": "ir.actions.client",
            "tag": "display_notification",
            "params": {
                "title": _("Готово!"),
                "message": _("Создано расписание с %s задачами.") % schedule.project_task_ids,
                "type": "success",
                "next": {
                    "type": "ir.actions.act_window",
                    "res_model": "task.schedule",
                    "res_id": schedule.id,
                    "views": [(False, "form")],
                },
            },
        }

    def _get_process_types(self):
        """Получение типов процессов для фильтрации шаблонов"""
        if self.process_type == "both":
            return ["main", "parallel"]
        return [self.process_type]

    def _get_shift_code(self, shift_number):
        """Определение кода смены по номеру"""
        shift_pattern = ["morning", "day", "night"]
        return shift_pattern[(shift_number - 1) % 3]

    def action_preview_schedule(self):
        """Предварительный просмотр расписания"""
        self.ensure_one()
        
        return {
            "type": "ir.actions.client",
            "tag": "display_notification",
            "params": {
                "title": _("Предварительный просмотр"),
                "message": _(
                    "Будет создано производственное расписание:\n"
                    "• Цех: %s\n"
                    "• Смен: %s\n"
                    "• Тип продукции: %s\n"
                    "• Объем: %s тонн\n"
                    "• Примерное количество работ: %s\n"
                    "• Период: с %s по %s"
                ) % (
                    self.project_id.name,
                    self.shift_count,
                    dict(self._fields["product_type"].selection).get(self.product_type),
                    self.production_volume,
                    self.estimated_tasks_count,
                    self.date_start,
                    self.estimated_end_date,
                ),
                "type": "info",
                "sticky": True,
            },
        }