from odoo import models, fields

class ProjectTaskTypeCustom(models.Model):
    _name = "project.task.type.custom"
    _description = "Пользовательские типы производственных операций"

    name = fields.Char(string="Название", required=True, translate=True)
    process_type = fields.Selection([
        ("main", "Основной техпроцесс"),
        ("parallel", "Вспомогательные работы"),
        ("maintenance", "Техническое обслуживание"),
    ], string="Тип процесса", required=True)
    
    category = fields.Char(string="Категория")
    sequence = fields.Integer(string="Порядок", default=10)
    active = fields.Boolean(default=True)