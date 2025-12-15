from odoo import models, fields

class ProjectProjectStage(models.Model):
    _inherit = 'project.project.stage'

    user_id = fields.Many2one(
        'hr.employee',
        string='Мастер смены',
        help='Ответственный мастер за этап производства',
        ondelete='set null',
        default=False,
    )