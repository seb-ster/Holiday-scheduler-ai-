"""Tiny Flask dashboard that fetches and displays Prometheus metrics.

This is a minimal separate dashboard app useful for quick inspection when
Prometheus is not available. It fetches the `/metrics` endpoint and renders
the raw metrics text inside a web page.
"""
from flask import Flask, Response
import requests

app = Flask(__name__)

METRICS_URL = "http://localhost:8000/metrics"


@app.route("/")
def index():
    try:
        r = requests.get(METRICS_URL, timeout=2.0)
        content = r.text
    except Exception as e:
        content = f"Error fetching metrics from {METRICS_URL}: {e}"
    return Response(f"<html><body><h1>Metrics</h1><pre>{content}</pre></body></html>", mimetype="text/html")


def main(port: int = 8080):
    app.run(host="0.0.0.0", port=port)


if __name__ == "__main__":
    main()
