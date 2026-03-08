"""Simple thread-based agent framework.

This package provides a minimal `AgentManager` and `ThreadAgent` implementation
so multiple agents can be spawned and run concurrently on separate threads.
"""

from .manager import AgentManager
from .thread_agent import ThreadAgent
from .base_agent import BaseAgent

__all__ = ["AgentManager", "ThreadAgent", "BaseAgent"]
