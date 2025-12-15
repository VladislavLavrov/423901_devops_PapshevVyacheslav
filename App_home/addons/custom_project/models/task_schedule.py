from odoo import fields, models


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