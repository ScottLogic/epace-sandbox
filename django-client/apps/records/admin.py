import json

from django import forms
from django.contrib import admin, messages
from django.utils.safestring import mark_safe

from .models import CSVFormatProfile, CSVUpload, PurchaseRecord, SalesRecord
from .services.csv_parser import DefaultCSVParser


class BaseRecordAdmin(admin.ModelAdmin):
    list_display = ("date", "item_name", "quantity", "unit_price", "total_price", "shipping_cost", "post_code", "currency")
    list_filter = ("date", "currency")
    search_fields = ("item_name", "post_code")
    readonly_fields = ("created_at",)


@admin.register(SalesRecord)
class SalesRecordAdmin(BaseRecordAdmin):
    pass


@admin.register(PurchaseRecord)
class PurchaseRecordAdmin(BaseRecordAdmin):
    pass


class CSVUploadForm(forms.ModelForm):
    class Meta:
        model = CSVUpload
        fields = "__all__"

    def clean_format_profile(self):
        profile = self.cleaned_data.get("format_profile")
        if not self.instance.pk and not profile:
            raise forms.ValidationError("A format profile is required for new uploads.")
        return profile

    def clean(self):
        cleaned_data = super().clean()
        record_type = cleaned_data.get("record_type")
        profile = cleaned_data.get("format_profile")
        if profile and record_type and profile.record_type != record_type:
            raise forms.ValidationError(
                "The selected format profile does not match the chosen record type."
            )
        return cleaned_data


@admin.register(CSVUpload)
class CSVUploadAdmin(admin.ModelAdmin):
    form = CSVUploadForm
    list_display = ("record_type", "format_profile", "uploaded_at", "rows_imported", "file")
    list_filter = ("record_type",)
    readonly_fields = ("uploaded_at", "rows_imported", "errors")

    def formfield_for_foreignkey(self, db_field, request, **kwargs):
        if db_field.name == "format_profile":
            kwargs["queryset"] = CSVFormatProfile.objects.filter(is_active=True)
        return super().formfield_for_foreignkey(db_field, request, **kwargs)

    def _get_profile_record_type_map(self):
        profiles = CSVFormatProfile.objects.filter(is_active=True).values_list(
            "id", "record_type"
        )
        return {str(pk): rt for pk, rt in profiles}

    def changeform_view(self, request, object_id=None, form_url="", extra_context=None):
        extra_context = extra_context or {}
        extra_context["profile_record_type_map"] = mark_safe(
            json.dumps(self._get_profile_record_type_map())
        )
        return super().changeform_view(request, object_id, form_url, extra_context)

    class Media:
        js = ("admin/js/csv_upload_filter.js",)

    def save_model(self, request, obj, form, change):
        super().save_model(request, obj, form, change)

        if not change:
            self._process_csv(request, obj)

    def _process_csv(self, request, obj):
        parser = DefaultCSVParser()

        try:
            file_content = obj.file.read().decode("utf-8-sig")
        except UnicodeDecodeError:
            obj.errors = "File encoding error. Please upload a UTF-8 encoded CSV."
            obj.save()
            self.message_user(request, obj.errors, messages.ERROR)
            return

        records, errors = parser.parse(file_content)

        model_class = SalesRecord if obj.record_type == "sales" else PurchaseRecord
        created = 0
        for record_data in records:
            model_class.objects.create(**record_data)
            created += 1

        obj.rows_imported = created
        obj.errors = "\n".join(errors) if errors else ""
        obj.save()

        if errors:
            self.message_user(
                request,
                f"Imported {created} records with {len(errors)} error(s). Check the upload details for more info.",
                messages.WARNING,
            )
        else:
            self.message_user(
                request,
                f"Successfully imported {created} {obj.record_type} records.",
                messages.SUCCESS,
            )


@admin.register(CSVFormatProfile)
class CSVFormatProfileAdmin(admin.ModelAdmin):
    list_display = ("name", "record_type", "delimiter", "is_active", "created_at", "updated_at")
    list_filter = ("record_type", "is_active")
    search_fields = ("name",)
    readonly_fields = ("created_at", "updated_at")
