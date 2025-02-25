name: "Bug Report"
description: "Report a bug or issue with Town of Host: Enhanced."
title: "[Bug]: "
labels: ["bug", "triage"]
body:
  - type: markdown
    attributes:
      value: |
        Thanks for taking the time to fill out this bug report!
  - type: input
    id: contact
    attributes:
      label: "Contact Details"
      description: "How can we get in touch with you if we need more info?"
      placeholder: "ex. email@example.com"
    validations:
      required: false
  - type: textarea
    id: description
    attributes:
      label: "Describe the bug"
      description: "A clear and concise description of what the bug is."
    validations:
      required: true
  - type: textarea
    id: steps-to-reproduce
    attributes:
      label: "Steps to Reproduce"
      description: "Steps to reproduce the behavior."
    validations:
      required: true
  - type: textarea
    id: expected-behavior
    attributes:
      label: "Expected Behavior"
      description: "What should have happened instead?"
    validations:
      required: true
  - type: dropdown
    id: platform
    attributes:
      label: "Platform"
      options:
        - Windows
        - Linux
        - MacOS
    validations:
      required: true
  - type: input
    id: version
    attributes:
      label: "Mod Version"
      description: "Which version of TOHE are you using?"
    validations:
      required: true
  - type: textarea
    id: logs
    attributes:
      label: "Logs & Crash Reports"
      description: "Paste any relevant logs or crash reports here."
      render: shell
    validations:
      required: false
  - type: checkboxes
    id: terms
    attributes:
      label: "Code of Conduct"
      description: "By submitting this issue, you agree to follow our [Code of Conduct](https://weareten.ca/code-of-conduct)."
      options:
        - label: "I agree to follow this project's Code of Conduct"
          required: true
