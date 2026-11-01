Optional one-shot tools: OCR, source import and evaluation. Expensive parsing jobs are not always-on services.

## Contract validation

Use an isolated environment; do not upgrade the user's global Python packages.
The pinned validator supports the existing Python 3.7 installation and JSON Schema
Draft 2020-12. The RFC 3339 dependency enables date-time format checks.

```powershell
python -m venv .venv-contracts
.venv-contracts/Scripts/python.exe -m pip install --isolated --index-url https://pypi.org/simple -r tools/requirements-contracts.txt
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
```

On Linux/macOS use `.venv-contracts/bin/python` for the environment's Python.
Validation uses synthetic examples only, without weather or model API calls.
