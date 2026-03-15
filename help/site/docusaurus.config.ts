import {themes as prismThemes} from "prism-react-renderer";
import type {Config} from "@docusaurus/types";

const config: Config = {
  title: "Procedo Help",
  tagline: "Product-style documentation for building and running Procedo workflows",
  url: "https://docs.procedo.dev",
  baseUrl: "/",
  organizationName: "procedo",
  projectName: "procedo-help",
  onBrokenLinks: "throw",
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: "throw"
    }
  },
  i18n: {
    defaultLocale: "en",
    locales: ["en", "zh-Hans"],
    localeConfigs: {
      en: {
        label: "English"
      },
      "zh-Hans": {
        label: "简体中文",
        htmlLang: "zh-CN"
      }
    }
  },
  presets: [
    [
      "classic",
      {
        docs: {
          routeBasePath: "/",
          sidebarPath: "./sidebars.ts"
        },
        blog: false,
        pages: false,
        theme: {
          customCss: "./src/css/custom.css"
        }
      }
    ]
  ],
  themeConfig: {
    navbar: {
      title: "Procedo Help",
      items: [
        {
          type: "docSidebar",
          sidebarId: "helpSidebar",
          position: "left",
          label: "Docs"
        },
        {
          type: "localeDropdown",
          position: "right"
        }
      ]
    },
    footer: {
      style: "dark",
      links: [
        {
          title: "Docs",
          items: [
            {
              label: "Introduction",
              to: "/"
            },
            {
              label: "Get Started",
              to: "/get-started/install-and-setup"
            }
          ]
        },
        {
          title: "Project",
          items: [
            {
              label: "Procedo Help",
              to: "/"
            }
          ]
        }
      ],
      copyright: `Copyright ${new Date().getFullYear()} Procedo`
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ["powershell", "yaml", "csharp", "json"]
    }
  }
};

export default config;
