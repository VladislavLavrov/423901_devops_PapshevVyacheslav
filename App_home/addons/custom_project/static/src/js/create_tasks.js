/** @odoo-module **/
import { CalendarController } from "@web/views/calendar/calendar_controller";
import { useService } from "@web/core/utils/hooks";
import { onMounted, onPatched, onWillUnmount } from "@odoo/owl";
import { registry } from "@web/core/registry";

const CUSTOM_CALENDAR_S_JS_CLASS = "custom_project_calendar_create_tasks";

export class CustomProjectCalendarController_S extends CalendarController {
    setup() {
        super.setup();
        this.actionService = useService("action");
        
        this.addButtonRetryTimer = null;
        this.isButtonAdded = false;

        console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] setup called.`);

        onMounted(() => {
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] onMounted triggered. this.el:`, this.el, "this.renderer:", this.renderer);
            this.scheduleAddButtonAttempt("onMounted");
        });

        onPatched(() => {
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] onPatched triggered. this.el:`, this.el);
            if (!this.isButtonAdded) {
                 this.scheduleAddButtonAttempt("onPatched");
            }
        });

        onWillUnmount(() => {
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] onWillUnmount triggered.`);
            if (this.addButtonRetryTimer) {
                clearTimeout(this.addButtonRetryTimer);
                this.addButtonRetryTimer = null;
            }
        });
    }

    scheduleAddButtonAttempt(source = "unknown") {
        if (this.addButtonRetryTimer) {
            clearTimeout(this.addButtonRetryTimer);
        }
        this.addButtonRetryTimer = setTimeout(() => {
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] scheduleAddButtonAttempt from ${source}: Вызов addButtonToCalendarButtons через setTimeout.`);
            this.addButtonToCalendarButtons();
        }, 0);
    }


    async onCustomCalendarButtonClick() {
    console.log("[custom_project_calendar_my] Кастомная кнопка календаря нажата! Будем вызывать action_open_custom_task_form2.");

    let projectId = null;

    try {
        // === 1. Получение project_id (аналогично твоему коду) ===
        const searchModel = this.env?.searchModel;

        if (searchModel?.context?.default_project_id) {
            projectId = searchModel.context.default_project_id;
            console.log(`[custom_project_calendar_my] Project ID из searchModel.context.default_project_id: ${projectId}`);
        } else if (searchModel?.context?.project_id) {
            projectId = searchModel.context.project_id;
            console.log(`[custom_project_calendar_my] Project ID из searchModel.context.project_id: ${projectId}`);
        } else if (searchModel?.domain) {
            const projectDomainPart = searchModel.domain.find(part =>
                Array.isArray(part) &&
                part[0] === 'project_id' &&
                part[1] === '=' &&
                typeof part[2] === 'number'
            );
            if (projectDomainPart) {
                projectId = projectDomainPart[2];
                console.log(`[custom_project_calendar_my] Project ID из домена: ${projectId}`);
            }
        }

        // Если задача открыта из контекста (редко для календаря)
        const { resModel, resId } = this.props;
        if (!projectId && resModel === 'project.task' && resId) {
            const taskData = await this.orm.call('project.task', 'read', [resId], ['project_id']);
            if (taskData?.[0]?.project_id) {
                projectId = taskData[0].project_id[0];
                console.log(`[custom_project_calendar_my] Project ID из задачи: ${projectId}`);
            }
        }
        
        const self = this;
        // === 2. Вызов существующего действия через его XML ID ===
        await this.actionService.doAction('custom_project.action_open_custom_task_form2', {
            additionalContext: {
                default_project_id: projectId || undefined,
                // Можно добавить другие значения по умолчанию
                // default_name: 'Серия задач',

            },
            onClose: async () => {
                // Эта функция вызовется, когда пользователь закроет форму
                console.log("[custom_project_calendar_my] Форма закрыта. Перезагружаем календарь...");
                window.location.reload();
            }
        });

    } catch (error) {
        console.error("[custom_project_calendar_my] Ошибка при вызове действия:", error);
        // Опционально: показ уведомления пользователю
        this.dialogService.add(AlertDialog, {
            title: "Ошибка",
            body: "Не удалось открыть форму. См. консоль.",
        });
    }
    }
    

    addButtonToCalendarButtons(attemptCount = 1) {
        const maxAttempts = 15; 
        const retryDelay = 150; 

        console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount} добавить кнопку.`);


        let rootElement = null;

        if (this.el) {
            rootElement = this.el;
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Нашли this.el.`);
        } 
        else if (this.renderer && typeof this.renderer.el !== 'undefined') {
            rootElement = this.renderer.el;
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Нашли this.renderer.el.`, rootElement);
        }
        else if (this.__owl__ && this.__owl__.bdom && this.__owl__.bdom.el) {
            rootElement = this.__owl__.bdom.el;
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Нашли this.__owl__.bdom.el.`, rootElement);
        }

        else {
             console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: this.el, this.renderer.el, this.__owl__.bdom.el не найдены.`);
             const potentialCalendars = document.querySelectorAll('.o_calendar_view, .o_calendar_widget');
             if (potentialCalendars.length > 0) {
                 console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Найдены потенциальные элементы календаря в document:`, potentialCalendars.length);
                 rootElement = potentialCalendars[0];
                 console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Используем первый найденный элемент календаря как root.`, rootElement);
             }
        }

        if (!rootElement) {
            console.warn(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Корневой элемент (rootElement) не доступен.`);
            if (attemptCount < maxAttempts) {
                console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Планируем повторную попытку через ${retryDelay}ms.`);
                this.addButtonRetryTimer = setTimeout(() => {
                    this.addButtonToCalendarButtons(attemptCount + 1);
                }, retryDelay);
            } else {
                console.error(`[${CUSTOM_CALENDAR_S_JS_CLASS}] После ${maxAttempts} попыток rootElement все еще не доступен. Прекращаем попытки.`);
                 console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Для отладки: this (часть):`, {
                    displayName: this.displayName,
                    props: this.props ? { resModel: this.props.resModel } : 'no props',
                    __owl__: this.__owl__ ? { status: this.__owl__.status } : 'no __owl__',
                    renderer: this.renderer ? 'exists' : 'null/undefined',
                    el: this.el
                });
            }
            return;
        }

        console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: rootElement доступен.`, rootElement);

        let calendarButtonsContainer = rootElement.querySelector('.o_calendar_buttons');

        if (!calendarButtonsContainer) {
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Прямой поиск .o_calendar_buttons внутри rootElement не удался.`);
            const allInDocument = document.querySelectorAll('.o_calendar_buttons');
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Найдено ${allInDocument.length} .o_calendar_buttons в document.`);
            for (const container of allInDocument) {
                if (rootElement === document || rootElement.contains(container)) {
                     calendarButtonsContainer = container;
                     console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Используем найденный .o_calendar_buttons.`, container);
                     break;
                }
            }
        }

        if (calendarButtonsContainer) {
            console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Найден calendarButtonsContainer.`, calendarButtonsContainer);
            const existingCustomSpan = calendarButtonsContainer.querySelector('.o_custom_calendar_button_span');
            if (existingCustomSpan) {
                console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Кнопка уже существует.`);
                this.isButtonAdded = true;
                return;
            }

            try {
                const customButtonSpan = document.createElement('span');
                customButtonSpan.className = 'o_custom_calendar_button_span me-1';

                const customButton = document.createElement('button');
                customButton.type = 'button';
                customButton.className = 'btn btn-secondary o_custom_calendar_button_from_js';
                customButton.textContent = 'Создать задачу';
                customButton.title = 'Создать задачу';
                customButton.setAttribute('aria-label', 'Создать задачу');

                customButton.addEventListener('click', (event) => {
                    event.preventDefault();
                    event.stopPropagation();
                    this.onCustomCalendarButtonClick();
                });

                customButtonSpan.appendChild(customButton);

                const firstSpan = calendarButtonsContainer.querySelector('span');
                if (firstSpan) {
                    calendarButtonsContainer.insertBefore(customButtonSpan, firstSpan);
                } else {
                    calendarButtonsContainer.appendChild(customButtonSpan);
                }

                this.isButtonAdded = true;
                console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Кнопка успешно добавлена.`);
                return;
            } catch (domError) {
                console.error(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Ошибка DOM:`, domError);
            }
        } else {
            console.warn(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Не найден .o_calendar_buttons.`);
            if (attemptCount < maxAttempts) {
                console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Планируем повторную попытку через ${retryDelay}ms.`);
                this.addButtonRetryTimer = setTimeout(() => {
                    this.addButtonToCalendarButtons(attemptCount + 1);
                }, retryDelay);
            } else {
                console.error(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Попытка #${attemptCount}: Достигнут лимит попыток. Остановка.`);
                 console.log(`[${CUSTOM_CALENDAR_S_JS_CLASS}] Для отладки: HTML rootElement (первые 500 символов):`, rootElement?.outerHTML?.substring(0, 500));
            }
        }
    }
}


import { calendarView } from "@web/views/calendar/calendar_view";

const customProjectCalendar_SView = {
    ...calendarView,
    Controller: CustomProjectCalendarController_S,
};

registry.category("views").add(CUSTOM_CALENDAR_S_JS_CLASS, customProjectCalendar_SView);

