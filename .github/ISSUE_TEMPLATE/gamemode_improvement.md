name: "Gamemode Improvement"
description: "Suggest balance changes or improvements for existing gamemodes."
labels: [balance, gamemode]
body:
  - type: input
    id: gamemode
    attributes:
      label: "Gamemode Name"
      description: "Which gamemode needs changes?"
    validations:
      required: true
  - type: textarea
    id: improvement-details
    attributes:
      label: "Improvement Details"
      description: "What changes should be made and why?"
    validations:
      required: true
  - type: textarea
    id: concerns
    attributes:
      label: "Possible Concerns"
      description: "Any potential balance issues?"
    validations:
      required: false
