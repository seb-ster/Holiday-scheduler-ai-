from typing import Any


class Model:
    """Tiny placeholder model.

    Replace with real model code. This implements a trivial predict method
    for demonstration and testing.
    """

    def __init__(self, params: Any = None):
        self.params = params or {}

    def train(self, X, y):
        # pretend we "fit" by storing the mean
        self.params["mean"] = sum(y) / len(y) if y else 0

    def predict(self, X):
        m = self.params.get("mean", 0)
        return [m for _ in X]
