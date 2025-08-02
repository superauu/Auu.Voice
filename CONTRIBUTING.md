# Contributing to AuuVoice

感谢您对 AuuVoice 项目的关注！我们欢迎所有形式的贡献，包括但不限于代码、文档、测试、反馈和建议。

Thank you for your interest in contributing to AuuVoice! We welcome all forms of contributions, including code, documentation, testing, feedback, and suggestions.

## 如何贡献 / How to Contribute

### 🐛 报告问题 / Reporting Issues

如果您发现了 bug 或有功能建议，请：
If you find a bug or have a feature suggestion, please:

1. 检查 [Issues](../../issues) 页面，确保问题尚未被报告
   Check the [Issues](../../issues) page to ensure the issue hasn't been reported

2. 创建新的 Issue，包含以下信息：
   Create a new Issue with the following information:
   - 清晰的标题和描述 / Clear title and description
   - 重现步骤 / Steps to reproduce
   - 预期行为 / Expected behavior
   - 实际行为 / Actual behavior
   - 系统环境信息 / System environment information
   - 相关截图或日志 / Relevant screenshots or logs

### 💻 代码贡献 / Code Contributions

#### 开发环境设置 / Development Environment Setup

1. **前置要求 / Prerequisites**
   - .NET 9.0 SDK
   - Visual Studio 2022 或 Visual Studio Code
   - Git

2. **克隆项目 / Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/AuuVoice.git
   cd AuuVoice
   ```

3. **安装依赖 / Install Dependencies**
   ```bash
   dotnet restore
   ```

4. **构建项目 / Build the Project**
   ```bash
   dotnet build
   ```

5. **运行项目 / Run the Project**
   ```bash
   dotnet run
   ```

#### 开发流程 / Development Workflow

1. **Fork 项目 / Fork the Repository**
   - 点击页面右上角的 "Fork" 按钮
   - Click the "Fork" button in the top-right corner

2. **创建功能分支 / Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **进行开发 / Make Your Changes**
   - 遵循现有的代码风格 / Follow existing code style
   - 添加必要的测试 / Add necessary tests
   - 更新相关文档 / Update relevant documentation

4. **提交更改 / Commit Your Changes**
   ```bash
   git add .
   git commit -m "feat: add your feature description"
   ```

5. **推送到您的 Fork / Push to Your Fork**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **创建 Pull Request / Create a Pull Request**
   - 提供清晰的 PR 描述 / Provide a clear PR description
   - 链接相关的 Issues / Link related issues
   - 确保所有检查通过 / Ensure all checks pass

### 📝 代码规范 / Code Standards

#### C# 编码规范 / C# Coding Standards

- 使用 PascalCase 命名类、方法和属性 / Use PascalCase for classes, methods, and properties
- 使用 camelCase 命名局部变量和参数 / Use camelCase for local variables and parameters
- 使用有意义的变量和方法名 / Use meaningful variable and method names
- 添加适当的注释和文档 / Add appropriate comments and documentation
- 遵循 Microsoft C# 编码约定 / Follow Microsoft C# coding conventions

#### 提交信息规范 / Commit Message Convention

使用 [Conventional Commits](https://www.conventionalcommits.org/) 格式：
Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
type(scope): description

[optional body]

[optional footer]
```

**类型 / Types:**
- `feat`: 新功能 / New feature
- `fix`: 修复 bug / Bug fix
- `docs`: 文档更新 / Documentation update
- `style`: 代码格式化 / Code formatting
- `refactor`: 代码重构 / Code refactoring
- `test`: 测试相关 / Test related
- `chore`: 构建或辅助工具变动 / Build or auxiliary tool changes

**示例 / Examples:**
```
feat(speech): add support for multiple languages
fix(ui): resolve window focus issue on first use
docs(readme): update installation instructions
```

### 🧪 测试 / Testing

在提交 PR 之前，请确保：
Before submitting a PR, please ensure:

- [ ] 所有现有测试通过 / All existing tests pass
- [ ] 新功能包含适当的测试 / New features include appropriate tests
- [ ] 代码覆盖率保持或提高 / Code coverage is maintained or improved
- [ ] 手动测试核心功能 / Manual testing of core functionality

### 📋 Pull Request 检查清单 / Pull Request Checklist

提交 PR 时，请确保：
When submitting a PR, please ensure:

- [ ] 代码遵循项目编码规范 / Code follows project coding standards
- [ ] 包含适当的测试 / Includes appropriate tests
- [ ] 文档已更新（如适用）/ Documentation is updated (if applicable)
- [ ] CHANGELOG.md 已更新 / CHANGELOG.md is updated
- [ ] 提交信息遵循约定格式 / Commit messages follow convention
- [ ] PR 描述清晰明了 / PR description is clear and comprehensive
- [ ] 链接了相关的 Issues / Related issues are linked

### 🎯 优先级领域 / Priority Areas

我们特别欢迎以下方面的贡献：
We especially welcome contributions in the following areas:

- **性能优化 / Performance Optimization**: 提升语音识别和文本处理速度
- **用户体验 / User Experience**: 改进界面设计和交互流程
- **多语言支持 / Multi-language Support**: 添加更多语言的支持
- **错误处理 / Error Handling**: 增强错误处理和用户反馈
- **文档完善 / Documentation**: 改进用户文档和开发者文档
- **测试覆盖 / Test Coverage**: 增加单元测试和集成测试
- **可访问性 / Accessibility**: 提升软件的可访问性

### 💬 交流讨论 / Communication

- **Issues**: 用于 bug 报告和功能请求 / For bug reports and feature requests
- **Discussions**: 用于一般讨论和问题 / For general discussions and questions
- **Email**: 对于敏感问题，可发送邮件至 [your-email@example.com]

### 📄 许可证 / License

通过贡献代码，您同意您的贡献将在 MIT 许可证下发布。
By contributing code, you agree that your contributions will be licensed under the MIT License.

### 🙏 致谢 / Acknowledgments

所有贡献者都将在项目的 README.md 文件中得到认可。
All contributors will be acknowledged in the project's README.md file.

---

再次感谢您的贡献！如果您有任何问题，请随时创建 Issue 或联系维护者。
Thank you again for your contribution! If you have any questions, please feel free to create an Issue or contact the maintainers.