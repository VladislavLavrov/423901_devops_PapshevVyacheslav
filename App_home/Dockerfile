FROM odoo:16.0

USER root

RUN mkdir -p /mnt/extra-addons \
    && chown -R odoo /mnt/extra-addons

COPY requirements.txt .

RUN pip install --default-timeout=1000 --retries=5 -r requirements.txt

COPY /addons /mnt/extra-addons

USER odoo