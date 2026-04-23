"""
Pytest configuration and fixtures.
"""
import pytest
import sys
import os

# Add src directory to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'src'))


@pytest.fixture(scope="session")
def anyio_backend():
    return "asyncio"
