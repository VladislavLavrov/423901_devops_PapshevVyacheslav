from odoo import fields, models, api
from odoo.exceptions import ValidationError


class TaskScheduleTemplate(models.Model):
    _name = "task.schedule.template"
    _description = "Шаблон расписания производственных задач"
    _order = "sequence, id"

    name = fields.Char(
        string="Название",
        required=False,
        compute="_compute_name",
        store=True,
        readonly=False,
    )
    
    sequence = fields.Integer(string="Порядок", default=10)
    
    process_type = fields.Selection(
        [
            ("main", "Основной техпроцесс"),
            ("parallel", "Вспомогательные работы"),
            ("maintenance", "Техническое обслуживание"),
        ],
        string="Тип процесса",
        required=True,
        default="main",
    )

    day_number = fields.Integer(string="День плана", required=True)

    shift = fields.Many2one(
        "metallurgy.shift", string="Смена", required=True
    )

    task_type_id = fields.Many2one(
        "project.task.type.custom", string="Тип работы", required=True
    )

    active = fields.Boolean(string="Активно", default=True)

    @api.depends("process_type", "day_number", "shift", "task_type_id")
    def _compute_name(self):
        for record in self:
            if record.task_type_id:
                record.name = f"{record.process_type} - День {record.day_number} - {record.shift.name} - {record.task_type_id.name}"
            else:
                record.name = f"{record.process_type} - День {record.day_number} - {record.shift.name}"

    @api.constrains("day_number")
    def _check_positive_values(self):
        for record in self:
            if record.day_number <= 0:
                raise ValidationError("Номер дня должен быть положительным числом")