name: "General Task"
description: "Use this for other development tasks (e.g., refactoring, documentation)."
labels: [task]
body:
  - type: textarea
    id: task-description
    attributes:
      label: "Task Description"
      description: "What needs to be done?"
    validations:
      required: true
  - type: textarea
    id: related-issues
    attributes:
      label: "Related Issues or PRs"
      description: "Link to related issues or pull requests."
    validations:
      required: false
