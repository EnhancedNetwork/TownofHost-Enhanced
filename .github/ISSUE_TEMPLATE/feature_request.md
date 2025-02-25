name: "Feature Request"
description: "Suggest a new feature or role for Town of Host: Enhanced."
labels: [enhancement]
body:
  - type: textarea
    id: feature-description
    attributes:
      label: "Feature Description"
      description: "Describe the new feature or role."
    validations:
      required: true
  - type: textarea
    id: potential-benefits
    attributes:
      label: "Potential Benefits"
      description: "How would this feature improve the mod?"
    validations:
      required: true
  - type: textarea
    id: potential-issues
    attributes:
      label: "Potential Issues"
      description: "Are there any balance concerns or conflicts?"
    validations:
      required: false
