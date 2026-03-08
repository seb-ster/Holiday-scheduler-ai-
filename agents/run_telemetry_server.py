"""Start a Prometheus metrics HTTP server for scraping.

Usage:
    python -m agents.run_telemetry_server --port 8000
"""
import argparse
import logging

log = logging.getLogger(__name__)

def main(port: int = 8000):
    try:
        from prometheus_client import start_http_server
        from .telemetry import init_metrics
    except Exception:
        log.exception("prometheus_client not available; install prometheus_client to enable telemetry")
        raise

    init_metrics()
    start_http_server(port)
    log.info("Prometheus metrics server running on :%d", port)
    # keep running
    try:
        import time

        while True:
            time.sleep(3600)
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--port", type=int, default=8000)
    args = p.parse_args()
    logging.basicConfig(level=logging.INFO)
    main(args.port)
