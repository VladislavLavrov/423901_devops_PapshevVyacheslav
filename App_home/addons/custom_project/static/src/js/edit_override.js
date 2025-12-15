// /** @odoo-module **/

// import { CalendarController } from "@web/views/calendar/calendar_controller";
// import { useService } from "@web/core/utils/hooks";
// import { calendarView } from "@web/views/calendar/calendar_view";
// import { registry } from "@web/core/registry";
// import { onMounted } from "@odoo/owl";

// export class MyProjectCalendarController extends CalendarController {
//     setup() {
//         super.setup();
//         this.actionService = useService("action");
        
//         onMounted(() => {
//             this.patchDoShow();
//         });
//     }

//     patchDoShow() {
//         // Сохраняем оригинальный метод
//         const originalDoShow = this.doShow.bind(this);
        
//         // Переопределяем метод
//         this.doShow = async (record) => {
//             // Проверяем, что это проектная задача
//             if (this.props.resModel === 'project.task') {
//                 console.log(this.props.resModel)
//                 // Вызываем кастомное действие
//                 await this.actionService.doAction('custom_project.action_open_custom_task_form2', {
//                     additionalContext: {
//                         active_id: record.id,
//                         active_ids: [record.id],
//                         default_res_id: record.id,
//                     }
//                 });
//                 return; // Прерываем стандартное поведение
//             }
            
//             // Для других моделей — стандартное поведение
//             return originalDoShow(record);
//         };
//     }
// }

// const customCalendarView = {
//     ...calendarView,
//     Controller: MyProjectCalendarController,
// };

// registry.category("views").add("project_task_calendar_custom", customCalendarView);


/** @odoo-module **/

import { CalendarController } from "@web/views/calendar/calendar_controller";
import { useService } from "@web/core/utils/hooks";
import { calendarView } from "@web/views/calendar/calendar_view";
import { registry } from "@web/core/registry";

export class MyProjectCalendarController extends CalendarController {
    setup() {
        super.setup();
        this.actionService = useService("action");
        this.dialog = useService("dialog");
    }

    /**
     * @override
     */
    async onEventClick(info) {
        // Проверяем, что это проектная задача
        if (this.props.resModel === 'project.task') {
            console.log('Custom event click for project task:', info.event);
            
            try {
                // Вызываем кастомное действие
                await this.actionService.doAction('custom_project.action_open_custom_task_form2', {
                    additionalContext: {
                        active_id: info.event.id,
                        active_ids: [info.event.id],
                        default_res_id: info.event.id,
                    }
                });
            } catch (error) {
                console.error('Error opening custom form:', error);
                this.dialog.add(this.env.services.dialog.Alert, {
                    title: "Error",
                    body: "Failed to open the form.",
                });
            }
            return; // Прерываем стандартное поведение
        }
        
        // Для других моделей — стандартное поведение
        return super.onEventClick(info);
    }

    /**
     * @override
     */
    async openRecord(record) {
        // Проверяем, что это проектная задача
        if (this.props.resModel === 'project.task') {
            console.log('Custom openRecord for project task:', record);
            
            try {
                // Вызываем кастомное действие
                await this.actionService.doAction('custom_project.action_open_custom_task_form2', {
                    additionalContext: {
                        active_id: record.id,
                        active_ids: [record.id],
                        default_res_id: record.id,
                    }
                });
            } catch (error) {
                console.error('Error opening custom form:', error);
                this.dialog.add(this.env.services.dialog.Alert, {
                    title: "Error",
                    body: "Failed to open the form.",
                });
            }
            return; // Прерываем стандартное поведение
        }
        
        // Для других моделей — стандартное поведение
        return super.openRecord(record);
    }
}

const customCalendarView = {
    ...calendarView,
    Controller: MyProjectCalendarController,
};

registry.category("views").add("project_task_calendar_edit_override", customCalendarView);