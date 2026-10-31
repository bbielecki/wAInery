#!/usr/bin/env python3
"""Gate 3 design-time JSON schema/fixture checks. Dev-only dependency: jsonschema."""
import json
from pathlib import Path
from jsonschema import Draft202012Validator, FormatChecker, ValidationError

ROOT = Path(__file__).resolve().parents[1]
PAIRS = {
    'contracts/events/domain-change.v1.schema.json': 'contracts/examples/domain-change.observation.json',
    'contracts/api/record-observation.v1.schema.json': 'contracts/examples/record-observation.numeric.json',
    'contracts/tools/agent-advice-request.v1.schema.json': 'contracts/examples/agent-advice-request.json',
    'contracts/tools/agent-advice-result.v1.schema.json': 'contracts/examples/agent-advice-result.json',
    'contracts/api/decision-plan.v1.schema.json': 'contracts/examples/decision-plan.json',
}

for schema_path, fixture_path in PAIRS.items():
    schema = json.loads((ROOT / schema_path).read_text(encoding='utf-8'))
    Draft202012Validator.check_schema(schema)
    fixture = json.loads((ROOT / fixture_path).read_text(encoding='utf-8'))
    Draft202012Validator(schema, format_checker=FormatChecker()).validate(fixture)
    print(f'PASS {fixture_path}')

# Negative tests, to prevent common contract regressions.
event_schema = json.loads((ROOT / 'contracts/events/domain-change.v1.schema.json').read_text())
event = json.loads((ROOT / PAIRS['contracts/events/domain-change.v1.schema.json']).read_text())
try:
    Draft202012Validator(event_schema).validate({**event, 'rawBookText': 'NO'})
except ValidationError:
    print('PASS reject book text in domain event')
else:
    raise AssertionError('Event contract must reject arbitrary book content')
result_schema = json.loads((ROOT / 'contracts/tools/agent-advice-result.v1.schema.json').read_text())
res = json.loads((ROOT / PAIRS['contracts/tools/agent-advice-result.v1.schema.json']).read_text())
try:
    Draft202012Validator(result_schema).validate({**res, 'predictedSuccessProbability': 0.95})
except ValidationError:
    print('PASS reject unsourced probability from LLM result')
else:
    raise AssertionError('Model result must not accept freeform probability')
print('Gate 3 contract fixtures valid')
