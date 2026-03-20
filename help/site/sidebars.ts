import type {SidebarsConfig} from "@docusaurus/plugin-content-docs";

const sidebars: SidebarsConfig = {
  helpSidebar: [
    {
      type: "category",
      label: "Overview",
      items: [
        "overview/introduction",
        "overview/why-procedo",
        "overview/core-concepts"
      ]
    },
    {
      type: "category",
      label: "Get Started",
      items: [
        "get-started/install-and-setup",
        "get-started/run-your-first-workflow",
        "get-started/create-your-first-workflow",
        "get-started/procedo-cli-basics"
      ]
    },
    {
      type: "category",
      label: "Author Workflows",
      items: [
        "author-workflows/workflow-structure-overview",
        "author-workflows/steps",
        "author-workflows/parameters",
        "author-workflows/variables",
        "author-workflows/outputs",
        "author-workflows/expressions-overview",
        "author-workflows/conditions"
      ]
    },
    {
      type: "category",
      label: "Run and Operate",
      items: [
        "run-and-operate/persistence",
        "run-and-operate/observability",
        "run-and-operate/validation"
      ]
    },
    {
      type: "category",
      label: "Templates",
      items: [
        "templates/templates-overview",
        "templates/template-parameters",
        "templates/template-conditions",
        "templates/template-loops",
        "templates/template-limitations"
      ]
    },
    {
      type: "category",
      label: "Extend Procedo",
      items: [
        "extend-procedo/plugin-authoring-overview",
        "extend-procedo/create-a-custom-step",
        "extend-procedo/method-binding",
        "extend-procedo/dependency-injection-integration"
      ]
    },
    {
      type: "category",
      label: "Use in .NET",
      items: [
        "use-in-dotnet/embedding-procedo",
        "use-in-dotnet/procedo-host-builder",
        "use-in-dotnet/execute-yaml-from-code",
        "use-in-dotnet/callback-driven-resume",
        "use-in-dotnet/custom-runtime-composition"
      ]
    },
    {
      type: "category",
      label: "Reference",
      items: [
        "reference/cli-overview",
        {
          type: "category",
          label: "YAML",
          items: [
            "reference/yaml-workflow-schema-overview",
            "reference/yaml-name-and-version",
            "reference/yaml-parameters",
            "reference/yaml-stages-jobs-steps",
            "reference/yaml-with-and-depends-on",
            "reference/yaml-condition"
          ]
        },
        {
          type: "category",
          label: "Expressions",
          items: [
            "reference/expressions-overview-reference",
            "reference/expressions-sources-and-resolution",
            "reference/expressions-functions",
            "reference/expressions-condition-rules"
          ]
        },
        {
          type: "category",
          label: "Built-in Steps",
          items: [
            "reference/built-in-steps-overview",
            "reference/built-in-steps-core-utilities",
            "reference/built-in-steps-data-formats",
            "reference/built-in-steps-http",
            "reference/built-in-steps-archive-and-hash",
            "reference/built-in-steps-file-and-directory",
            "reference/built-in-steps-process-and-security",
            "reference/built-in-steps-wait-and-resume",
            "reference/built-in-steps-secure-runtime"
          ]
        },
        {
          type: "category",
          label: "Runtime",
          items: [
            "reference/runtime-statuses",
            "reference/runtime-persistence-state",
            "reference/runtime-error-codes"
          ]
        }
      ]
    },
    {
      type: "category",
      label: "Recipes",
      items: [
        "recipes/minimal-pipeline",
        "recipes/passing-data-between-steps",
        "recipes/conditional-execution",
        "recipes/dependencies-and-execution-order"
      ]
    },
    {
      type: "category",
      label: "Troubleshooting",
      items: [
        "troubleshooting/common-validation-errors",
        "troubleshooting/faq"
      ]
    },
    {
      type: "category",
      label: "What's New",
      items: [
        "whats-new/release-notes-index",
        "whats-new/phase-1-release-notes",
        "whats-new/known-limitations",
        "whats-new/support-matrix",
        "whats-new/roadmap"
      ]
    }
  ]
};

export default sidebars;
