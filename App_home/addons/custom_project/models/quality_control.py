from odoo import fields, models

class QualityControl(models.Model):
    _name = "quality.control"
    _description = "Контроль качества продукции"
    
    name = fields.Char(string="Номер акта", required=True)
    task_id = fields.Many2one("project.task", string="Производственная задача", required=True)
    
    inspector_id = fields.Many2one("hr.employee", string="Контролёр ОТК", required=True)
    inspection_datetime = fields.Datetime(string="Время контроля", default=fields.Datetime.now)
    
    status = fields.Selection([
        ('pending', 'Ожидает проверки'),
        ('accepted', 'Принято'),
        ('rejected', 'Забраковано')
    ], string="Результат контроля", default="pending", required=True)
    
    parameters = fields.Text(string="Контролируемые параметры")
    notes = fields.Text(string="Замечания")
    measurement_data = fields.Text(string="Данные измерений")
    
    product_batch = fields.Char(string="Партия продукции")
    certificate_number = fields.Char(string="Номер сертификата")
    
    def action_accept(self):
        self.status = 'accepted'
        
    def action_reject(self):
        self.status = 'rejected'