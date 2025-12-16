from odoo import fields, models
from datetime import timedelta
from odoo.exceptions import UserError


class TaskSchedule(models.Model):
    _name = "task.schedule"
    _description = "Расписание производственных задач"

    name = fields.Char(string="Название расписания", required=True)
    project_id = fields.Many2one("project.project", string="Цех/Участок", required=True)

    # Параметры планирования
    start_shift = fields.Selection(
        [("1", "Смена 1 (Утренняя)"), 
         ("2", "Смена 2 (Дневная)"), 
         ("3", "Смена 3 (Ночная)")],
        string="Начать планирование со смены",
        required=True,
        default="1",
    )

    shift_count = fields.Selection(
        [(str(i), str(i)) for i in range(1, 9)],
        string="Количество смен для планирования",
        required=True,
        default="1",
    )

    equipment_maintenance_days = fields.Integer(
        string="Дней на ТО оборудования", default=0, required=True
    )

    process_type = fields.Selection(
        [
            ("main", "Основной техпроцесс"),
            ("parallel", "Вспомогательные работы"),
            ("both", "Оба")
        ],
        string="Тип процесса",
        required=True,
        default="main",
    )

    planning_series_datetime = fields.Datetime(
        string="Дата и время создания серии", readonly=True, default=fields.Datetime.now
    )

    date_start = fields.Datetime(
        string="Дата начала планирования", required=True, default=fields.Datetime.now
    )

    # Связь с задачами
    project_task_ids = fields.One2many(
        "project.task", "schedule_id", string="Задачи расписания"
    )

    def action_delete_schedule_series(self):
        """Удалить всю серию расписаний и задач"""
        for schedule in self:
            # Удаляем все связанные задачи
            self.project_task_ids.unlink()
            # Удаляем само расписание
            schedule.unlink()

        return self._show_success_notification(
            f"Успешно удалено расписаний: {len(self)}."
        )

    def _show_success_notification(self, message):
        """Вспомогательный метод для показа уведомлений"""
        return {
            "type": "ir.actions.client",
            "tag": "display_notification",
            "params": {
                "title": "Успех!",
                "message": message,
                "type": "success",
                "sticky": False,
                "next": {"type": "ir.actions.client", "tag": "reload"},
            },
        }


    # В models/task_schedule.py

    def action_generate_tasks(self):
        """Генерация задач из шаблонов расписания"""
        self.ensure_one()

        if not self.date_start:
            raise UserError("Укажите дату начала расписания!")

        # Определяем типы процессов для выборки шаблонов
        if self.process_type == "both":
            process_types = ["main", "parallel"]
        else:
            process_types = [self.process_type]

        templates = self.env["task.schedule.template"].search([
            ("process_type", "in", process_types),
            ("active", "=", True),
        ]).sorted("sequence")

        if not templates:
            raise UserError("Не найдено активных шаблонов для указанных типов процессов!")

        task_vals_list = []
        current_datetime = fields.Datetime.to_datetime(self.date_start)

        # Получаем все смены
        shifts = self.env["custom_project.shift"].search([], order="start_hour")
        shift_cycle = shifts  # [morning, day, night]

        total_shifts = int(self.shift_count)
        start_index = int(self.start_shift) - 1  # 0-based

        for i in range(total_shifts):
            # Определяем текущую смену по циклу
            shift_index = (start_index + i) % len(shift_cycle)
            shift = shift_cycle[shift_index]

            # Корректируем дату: каждая смена — +8 часов
            if i == 0:
                # Первая смена начинается в start_hour текущей даты
                task_start = current_datetime.replace(
                    hour=shift.start_hour, minute=0, second=0, microsecond=0
                )
            else:
                task_start = current_datetime + timedelta(hours=8 * i) if current_datetime else timedelta(hours=8 * i)

            # Для ночной смены, начинающейся в 22:00, следующий день начинается утром
            # Но для простоты оставим линейное приращение

            for template in templates.filtered(lambda t: t.process_type in process_types and t.shift.id == shift.id):
                task_vals = {
                    "name": template.task_type_id.name,
                    "project_id": self.project_id.id,
                    "schedule_id": self.id,
                    "process_type": template.process_type,
                    "shift": shift.id,
                    "date_start": task_start,
                    "custom_task_type_id": template.task_type_id.id,
                    "planning_series_datetime": fields.Datetime.now(),
                    "shift_number": str(i + int(self.start_shift)),
                    "stage_id": self.env.ref("custom_project.project_task_stage_planned").id,
                }

                # Если указаны дни ТО
                if self.equipment_maintenance_days > 0:
                    task_vals["maintenance_day"] = i + 1  # или другая логика

                task_vals_list.append(task_vals)

        if task_vals_list:
            self.env["project.task"].create(task_vals_list)

        return True