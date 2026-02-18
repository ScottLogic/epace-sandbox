from datetime import date, timedelta

from django.shortcuts import render

from .services.aggregation import AggregationService


def dashboard(request):
    start_date_str = request.GET.get("start_date", "")
    end_date_str = request.GET.get("end_date", "")

    today = date.today()
    default_start = today - timedelta(days=30)

    try:
        start_date = date.fromisoformat(start_date_str) if start_date_str else default_start
    except ValueError:
        start_date = default_start

    try:
        end_date = date.fromisoformat(end_date_str) if end_date_str else today
    except ValueError:
        end_date = today

    search_term = request.GET.get("q", "").strip()

    summary = AggregationService.get_summary(start_date, end_date, search_term)
    sales = AggregationService.get_sales(start_date, end_date, search_term)
    purchases = AggregationService.get_purchases(start_date, end_date, search_term)

    context = {
        "start_date": start_date.isoformat(),
        "end_date": end_date.isoformat(),
        "search_term": search_term,
        "summary": summary,
        "sales": sales,
        "purchases": purchases,
    }
    return render(request, "records/dashboard.html", context)
