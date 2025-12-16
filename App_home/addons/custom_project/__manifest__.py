{
    'name': "Планирование работ на металлургическом комбинате",          # Название модуля
    'version': "1.0",                   # Версия модуля
    'author': "Your Name",              # Автор
    'depends': ['hr', 'project', 'web'],
    #'category': "Agriculture",               # Категория (Custom, Sales, HR и т.д.)
    'summary': "Планирование работ на МК. Составление плана.", # Краткое описание
    'description': """                 # Полное описание (многострочное)
        Модуль адаптирует функционал модуля project под нужды металлургического комбината
    """,
    'data': [
        'security/ir.model.access.csv',
        'data/shifts.xml',
        'data/stages.xml',
        
        'views/project_calendar_views.xml',
        'data/shedule_data.xml',
        'wizard/task_series_wizard.xml'
    ],
    'assets': {
        'web.assets_backend': [
            'custom_project/static/src/js/create_tasks.js',
            'custom_project/static/src/js/create_series.js',
            'custom_project/static/src/js/edit_override.js'
        ],
    },
    'demo': [],                        # Демо-данные (опционально)
    'installable': True,               # Возможность установки
    'application': True,               # Приложение (отображается в Apps)
    # 'auto_install': True,
    'license': "LGPL-3",               # Лицензия
}
