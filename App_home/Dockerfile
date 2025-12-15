FROM odoo:16.0

USER root

RUN mkdir -p /mnt/extra-addons \
    && chown -R odoo /mnt/extra-addons

COPY requirements.txt .

RUN pip install -r requirements.txt

COPY /addons /mnt/extra-addons

USER odoo