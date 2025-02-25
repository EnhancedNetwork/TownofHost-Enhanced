name: "Bug Report"
description: "Report a bug or issue with Town of Host: Enhanced."
labels: [bug]
body:
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
  - type: textarea
    id: logs
    attributes:
      label: "Logs & Crash Reports"
      description: "Paste any relevant logs or crash reports here."
    validations:
      required: false
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
