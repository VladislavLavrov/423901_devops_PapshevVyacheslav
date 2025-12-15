from odoo import fields, models

class custom_projectShift(models.Model):
    _name = "custom_project.shift"
    _description = "Производственные смены металлургического комбината"
    
    name = fields.Char(string="Наименование смены", required=True)
    code = fields.Selection([
        ('morning', 'Утренняя (06:00-14:00)'),
        ('day', 'Дневная (14:00-22:00)'),
        ('night', 'Ночная (22:00-06:00)')
    ], string="Код смены", required=True)
    
    start_hour = fields.Integer(string="Час начала", required=True)
    end_hour = fields.Integer(string="Час окончания", required=True)
    
    active = fields.Boolean(string="Активна", default=True)
    
    # Связи
    task_ids = fields.One2many("project.task", "shift", string="Задачи смены")
    template_ids = fields.One2many("task.schedule.template", "shift", string="Шаблоны")