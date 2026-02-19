import csv
import io
from abc import ABC, abstractmethod
from datetime import datetime
from decimal import Decimal, InvalidOperation
from typing import Any


class CSVParser(ABC):
    @abstractmethod
    def parse(self, file_content: str) -> tuple[list[dict[str, Any]], list[str]]:
        pass


class DefaultCSVParser(CSVParser):
    REQUIRED_FIELDS = [
        "date",
        "item_name",
        "quantity",
        "unit_price",
        "total_price",
        "shipping_cost",
        "post_code",
    ]
    OPTIONAL_FIELDS = ["currency"]
    DATE_FORMATS = ["%Y-%m-%d", "%d/%m/%Y", "%m/%d/%Y"]

    def __init__(self, delimiter=None, date_format=None, field_mappings=None):
        self.delimiter = delimiter
        self.date_format = date_format
        self.field_mappings = field_mappings

    def parse(self, file_content: str) -> tuple[list[dict[str, Any]], list[str]]:
        if self.field_mappings:
            return self._parse_with_mappings(file_content)
        return self._parse_with_headers(file_content)

    def _parse_with_headers(self, file_content: str) -> tuple[list[dict[str, Any]], list[str]]:
        records = []
        errors = []

        reader = csv.DictReader(io.StringIO(file_content))

        if reader.fieldnames is None:
            errors.append("CSV file is empty or has no header row.")
            return records, errors

        normalised_fieldnames = [f.strip().lower() for f in reader.fieldnames]
        missing = [
            f for f in self.REQUIRED_FIELDS if f not in normalised_fieldnames
        ]
        if missing:
            errors.append(f"Missing required columns: {', '.join(missing)}")
            return records, errors

        for row_num, row in enumerate(reader, start=2):
            normalised_row = {k.strip().lower(): v.strip() for k, v in row.items()}
            row_errors = []
            parsed = {}

            parsed_date = self._parse_date(normalised_row.get("date", ""))
            if parsed_date is None:
                row_errors.append("invalid date format")
            else:
                parsed["date"] = parsed_date

            item_name = normalised_row.get("item_name", "").strip()
            if not item_name:
                row_errors.append("item_name is required")
            else:
                parsed["item_name"] = item_name

            for field in ("quantity",):
                val = normalised_row.get(field, "").strip()
                try:
                    parsed[field] = int(val)
                    if parsed[field] < 0:
                        row_errors.append(f"{field} must be non-negative")
                except (ValueError, TypeError):
                    row_errors.append(f"invalid {field}")

            for field in ("unit_price", "total_price", "shipping_cost"):
                val = normalised_row.get(field, "").strip()
                try:
                    parsed[field] = Decimal(val)
                except (InvalidOperation, TypeError):
                    row_errors.append(f"invalid {field}")

            post_code = normalised_row.get("post_code", "").strip()
            if not post_code:
                row_errors.append("post_code is required")
            else:
                parsed["post_code"] = post_code

            currency = normalised_row.get("currency", "").strip()
            parsed["currency"] = currency if currency else "GBP"

            if row_errors:
                errors.append(f"Row {row_num}: {'; '.join(row_errors)}")
            else:
                records.append(parsed)

        return records, errors

    def _parse_with_mappings(self, file_content: str) -> tuple[list[dict[str, Any]], list[str]]:
        records = []
        errors = []

        delimiter = self.delimiter or ","
        if delimiter == "\\t":
            delimiter = "\t"

        reader = csv.reader(io.StringIO(file_content), delimiter=delimiter)
        rows = list(reader)

        if not rows:
            errors.append("CSV file is empty or has no header row.")
            return records, errors

        csv_headers = rows[0]
        data_rows = rows[1:]

        if not data_rows:
            errors.append("CSV file has headers but no data rows.")
            return records, errors

        for row_num, row in enumerate(data_rows, start=2):
            row_errors = []
            parsed = {}

            for col_index_str, field_name in self.field_mappings.items():
                col_index = int(col_index_str)
                if col_index >= len(row):
                    row_errors.append(f"column {col_index} out of range")
                    continue
                raw_value = row[col_index].strip()

                if field_name == "date":
                    parsed_date = self._parse_date(raw_value)
                    if parsed_date is None:
                        row_errors.append("invalid date format")
                    else:
                        parsed["date"] = parsed_date
                elif field_name == "item_name":
                    if not raw_value:
                        row_errors.append("item_name is required")
                    else:
                        parsed["item_name"] = raw_value
                elif field_name == "quantity":
                    try:
                        parsed["quantity"] = int(raw_value)
                        if parsed["quantity"] < 0:
                            row_errors.append("quantity must be non-negative")
                    except (ValueError, TypeError):
                        row_errors.append("invalid quantity")
                elif field_name in ("unit_price", "total_price", "shipping_cost"):
                    try:
                        parsed[field_name] = Decimal(raw_value)
                    except (InvalidOperation, TypeError):
                        row_errors.append(f"invalid {field_name}")
                elif field_name == "post_code":
                    if not raw_value:
                        row_errors.append("post_code is required")
                    else:
                        parsed["post_code"] = raw_value
                elif field_name == "currency":
                    parsed["currency"] = raw_value if raw_value else "GBP"
                else:
                    parsed[field_name] = raw_value

            if "currency" not in parsed:
                parsed["currency"] = "GBP"

            if row_errors:
                errors.append(f"Row {row_num}: {'; '.join(row_errors)}")
            else:
                records.append(parsed)

        return records, errors

    def _parse_date(self, value: str) -> datetime | None:
        value = value.strip()
        if not value:
            return None
        formats = [self.date_format] if self.date_format else self.DATE_FORMATS
        for fmt in formats:
            try:
                return datetime.strptime(value, fmt).date()
            except ValueError:
                continue
        return None
