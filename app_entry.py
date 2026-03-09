"""Application entry point for building a single-file executable.

This script runs the demo agent runner. It is intentionally minimal so it
works well as a PyInstaller entry point. It also performs a conservative
update check at startup via `tools.auto_updater.updater` so operators can
download a release zip for manual inspection and application.
"""

from agents.threadagents.run_agents import main


def _maybe_check_updates():
    try:
        # Local import to avoid adding startup overhead when not available
        from tools.auto_updater import updater

        updater.check_and_prompt_update()
    except Exception:
        # If updater is not available or the environment is non-interactive,
        # continue without blocking startup.
        pass


if __name__ == "__main__":
    _maybe_check_updates()
    main()
