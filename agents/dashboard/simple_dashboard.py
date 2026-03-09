"""Tiny Flask dashboard that fetches and displays Prometheus metrics.

This is a minimal separate dashboard app useful for quick inspection when
Prometheus is not available. It fetches the `/metrics` endpoint and renders
the raw metrics text inside a web page.
"""
from flask import Flask, Response
import requests

app = Flask(__name__)

METRICS_URL = "http://localhost:8000/metrics"

try:
    from ..ui_notifier import default_notifier
    ui_available = True
except Exception:
    default_notifier = None
    ui_available = False


@app.route("/")
def index():
    # Prefer an in-memory snapshot from the notifier (non-blocking) if available.
    if ui_available and default_notifier is not None:
        snap = default_notifier.get_snapshot()
        # render snapshot as simple HTML
        body = "\n".join(f"{k}: {v}" for k, v in snap.items()) or "(no events yet)"
        return Response(f"<html><body><h1>Agent Snapshot</h1><pre>{body}</pre></body></html>", mimetype="text/html")

    # Fallback: fetch Prometheus metrics (with short timeout so UI doesn't block)
    try:
        r = requests.get(METRICS_URL, timeout=0.8)
        content = r.text
    except Exception as e:
        content = f"Error fetching metrics from {METRICS_URL}: {e}"
    return Response(f"<html><body><h1>Metrics</h1><pre>{content}</pre></body></html>", mimetype="text/html")


def main(port: int = 8080):
    app.run(host="0.0.0.0", port=port)


if __name__ == "__main__":
    main()
