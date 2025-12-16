from odoo import models, fields

class ProjectTaskTypeCustom(models.Model):
    _inherit = "project.task.type" 

    process_type = fields.Selection([
        ("main", "Основной техпроцесс"),
        ("parallel", "Вспомогательные работы"),
        ("maintenance", "Техническое обслуживание"),
    ], string="Тип процесса", required=True)
    
    category = fields.Char(string="Категория")