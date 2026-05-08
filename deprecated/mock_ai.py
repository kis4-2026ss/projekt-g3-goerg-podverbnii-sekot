#!/usr/bin/env python3
import json
import sys

# The first argument is the input prompt file path
# We just output a fixed JSON finding to test parsing
print(json.dumps({
    "summary": "Mock review for testing.",
    "findings": [
        {
            "file": "DomainName.java",
            "line": 5,
            "comment": "Mock finding for testing line parsing."
        }
    ]
}))
