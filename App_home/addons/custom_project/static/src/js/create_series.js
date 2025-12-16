/** @odoo-module **/
import { CalendarController } from "@web/views/calendar/calendar_controller";
import { useService } from "@web/core/utils/hooks";
import { onMounted, onPatched, onWillUnmount } from "@odoo/owl";
import { registry } from "@web/core/registry";

const CUSTOM_CALENDAR_JS_CLASS = "custom_project_calendar_create_series";

export class CustomProjectCalendarController extends CalendarController {
    setup() {
        super.setup();
        this.actionService = useService("action");
        
        this.addButtonRetryTimer = null;
        this.isButtonAdded = false;

        console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] setup called.`);

        onMounted(() => {
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] onMounted triggered. this.el:`, this.el, "this.renderer:", this.renderer);
            this.scheduleAddButtonAttempt("onMounted");
        });

        onPatched(() => {
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] onPatched triggered. this.el:`, this.el);
            if (!this.isButtonAdded) {
                 this.scheduleAddButtonAttempt("onPatched");
            }
        });

        onWillUnmount(() => {
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] onWillUnmount triggered.`);
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
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] scheduleAddButtonAttempt from ${source}: Вызов addButtonToCalendarButtons через setTimeout.`);
            this.addButtonToCalendarButtons();
        }, 0);
    }

    

    async onCustomCalendarButtonClick() {
        // Получаем XML ID действия визарда из вашего модуля
        const action = await this.actionService.loadAction("custom_project.action_task_series_wizard");
        await this.actionService.doAction(action);
    }

    addButtonToCalendarButtons(attemptCount = 1) {
        const maxAttempts = 15; 
        const retryDelay = 150; 

        console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount} добавить кнопку.`);


        let rootElement = null;

        if (this.el) {
            rootElement = this.el;
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Нашли this.el.`);
        } 
        else if (this.renderer && typeof this.renderer.el !== 'undefined') {
            rootElement = this.renderer.el;
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Нашли this.renderer.el.`, rootElement);
        }
        else if (this.__owl__ && this.__owl__.bdom && this.__owl__.bdom.el) {
            rootElement = this.__owl__.bdom.el;
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Нашли this.__owl__.bdom.el.`, rootElement);
        }

        else {
             console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: this.el, this.renderer.el, this.__owl__.bdom.el не найдены.`);
             const potentialCalendars = document.querySelectorAll('.o_calendar_view, .o_calendar_widget');
             if (potentialCalendars.length > 0) {
                 console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Найдены потенциальные элементы календаря в document:`, potentialCalendars.length);
                 rootElement = potentialCalendars[0];
                 console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Используем первый найденный элемент календаря как root.`, rootElement);
             }
        }

        if (!rootElement) {
            console.warn(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Корневой элемент (rootElement) не доступен.`);
            if (attemptCount < maxAttempts) {
                console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Планируем повторную попытку через ${retryDelay}ms.`);
                this.addButtonRetryTimer = setTimeout(() => {
                    this.addButtonToCalendarButtons(attemptCount + 1);
                }, retryDelay);
            } else {
                console.error(`[${CUSTOM_CALENDAR_JS_CLASS}] После ${maxAttempts} попыток rootElement все еще не доступен. Прекращаем попытки.`);
                 console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Для отладки: this (часть):`, {
                    displayName: this.displayName,
                    props: this.props ? { resModel: this.props.resModel } : 'no props',
                    __owl__: this.__owl__ ? { status: this.__owl__.status } : 'no __owl__',
                    renderer: this.renderer ? 'exists' : 'null/undefined',
                    el: this.el
                });
            }
            return;
        }

        console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: rootElement доступен.`, rootElement);

        let calendarButtonsContainer = rootElement.querySelector('.o_calendar_buttons');

        if (!calendarButtonsContainer) {
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Прямой поиск .o_calendar_buttons внутри rootElement не удался.`);
            const allInDocument = document.querySelectorAll('.o_calendar_buttons');
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Найдено ${allInDocument.length} .o_calendar_buttons в document.`);
            for (const container of allInDocument) {
                if (rootElement === document || rootElement.contains(container)) {
                     calendarButtonsContainer = container;
                     console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Используем найденный .o_calendar_buttons.`, container);
                     break;
                }
            }
        }

        if (calendarButtonsContainer) {
            console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Найден calendarButtonsContainer.`, calendarButtonsContainer);
            const existingCustomSpan = calendarButtonsContainer.querySelector('.o_custom_calendar_button_span');
            if (existingCustomSpan) {
                console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Кнопка уже существует.`);
                this.isButtonAdded = true;
                return;
            }

            try {
                const customButtonSpan = document.createElement('span');
                customButtonSpan.className = 'o_custom_calendar_button_span_series me-1';

                const customButton = document.createElement('button');
                customButton.type = 'button';
                customButton.className = 'btn btn-secondary o_custom_calendar_button_series_from_js';
                customButton.textContent = 'Создать задачу';
                customButton.title = 'Создать серию задач';
                customButton.setAttribute('aria-label', 'Создать серию задач');

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
                console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Кнопка успешно добавлена.`);
                return;
            } catch (domError) {
                console.error(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Ошибка DOM:`, domError);
            }
        } else {
            console.warn(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Не найден .o_calendar_buttons.`);
            if (attemptCount < maxAttempts) {
                console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Планируем повторную попытку через ${retryDelay}ms.`);
                this.addButtonRetryTimer = setTimeout(() => {
                    this.addButtonToCalendarButtons(attemptCount + 1);
                }, retryDelay);
            } else {
                console.error(`[${CUSTOM_CALENDAR_JS_CLASS}] Попытка #${attemptCount}: Достигнут лимит попыток. Остановка.`);
                 console.log(`[${CUSTOM_CALENDAR_JS_CLASS}] Для отладки: HTML rootElement (первые 500 символов):`, rootElement?.outerHTML?.substring(0, 500));
            }
        }
    }
}


import { calendarView } from "@web/views/calendar/calendar_view";

const customProjectCalendarView = {
    ...calendarView,
    Controller: CustomProjectCalendarController,
};

registry.category("views").add(CUSTOM_CALENDAR_JS_CLASS, customProjectCalendarView);

