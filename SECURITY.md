# Security Policy

## Reporting a Vulnerability

If you believe you have found a security issue in Crimson, please do not open a public issue with full exploit details.

Instead:

- open a private security advisory on GitHub if available
- or contact the project maintainers privately before public disclosure

Please include:

- affected version or commit
- reproduction steps
- impact assessment
- any suggested remediation

## Scope

At this stage, Crimson is early-stage tooling software. Security-sensitive areas likely include:

- parsing untrusted `.idl` input
- generation and file materialization logic
- merge and external tool invocation behavior
