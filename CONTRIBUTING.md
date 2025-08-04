# Contributing to BlitzCache

🎉 **Thank you for your interest in contributing to BlitzCache!** 

BlitzCache is a community-driven project, and we welcome contributions of all kinds - from bug reports and feature requests to code improvements and documentation updates.

## 🚀 Quick Start

1. **Fork** the repository
2. **Clone** your fork: `git clone https://github.com/yourusername/blitzcache.git`
3. **Create** a feature branch: `git checkout -b feature/amazing-feature`
4. **Make** your changes
5. **Test** thoroughly 
6. **Commit** your changes: `git commit -m 'Add amazing feature'`
7. **Push** to the branch: `git push origin feature/amazing-feature`
8. **Open** a Pull Request

## 📋 Types of Contributions

### 🐛 **Bug Reports**
Found a bug? Please check existing issues first, then:
- Use the **Bug Report** template
- Include minimal reproduction steps
- Specify your environment (.NET version, OS, BlitzCache version)
- Include relevant code snippets

### 💡 **Feature Requests**
Have an idea? We'd love to hear it!
- Use the **Feature Request** template
- Describe the problem you're solving
- Explain why this would benefit other users
- Consider implementation complexity

### 🔧 **Code Contributions**
Ready to dive in? Great!
- Check [open issues](https://github.com/chanido/blitzcache/issues) for ideas
- Comment on the issue you'd like to work on
- Follow our coding standards (see below)
- Add tests for new functionality
- Update documentation as needed

### 📚 **Documentation**
Help make BlitzCache easier to use:
- Fix typos and improve clarity
- Add examples and tutorials
- Improve XML documentation
- Update README or wikis

## 🎯 Development Guidelines

### **Environment Setup**
```bash
# Prerequisites
- .NET 6.0 SDK or later
- Visual Studio 2022 or VS Code
- Git

# Clone and setup
git clone https://github.com/chanido/blitzcache.git
cd blitzcache
dotnet restore
dotnet build
dotnet test
```

### **Coding Standards**
We follow C# best practices with some BlitzCache-specific conventions:

#### ✅ **Code Style**
- **Simplicity > Dogma**: Use the simplest solution that works
- **YAGNI**: Don't add abstractions for hypothetical future needs
- **Readability First**: Prefer readable code over performance micro-optimizations
- **Single Responsibility**: Methods should do one thing well

#### ✅ **Naming Conventions**
- Use `camelCase` for private fields (no underscore prefix)
- Use `PascalCase` for public members
- Use descriptive names: `GetCachedValue` not `GetVal`
- Avoid abbreviations unless widely understood

#### ✅ **Method Style**
- Use `=>` syntax for single-line methods when possible
- One line per concept when reasonable
- Prefer `switch` expressions over traditional switch statements
- Use pattern matching when it improves readability

#### ✅ **Comments & Documentation**
- Explain **why**, not **what**
- Avoid obvious comments
- Use XML documentation for public APIs
- Include usage examples in XML docs

### **Testing Requirements**
- ✅ **Unit Tests**: All new functionality must have tests
- ✅ **Integration Tests**: Test real-world scenarios
- ✅ **Performance Tests**: Verify no regressions
- ✅ **Thread Safety**: Test concurrent scenarios

```csharp
[Test]
public async Task BlitzGet_Should_Handle_Concurrent_Requests()
{
    // Arrange
    var cache = TestHelpers.CreateBasic();
    var callCount = 0;
    
    // Act
    var tasks = Enumerable.Range(0, 100).Select(_ => 
        cache.BlitzGet("test-key", () => {
            Interlocked.Increment(ref callCount);
            return "result";
        }, 1000));
    
    var results = await Task.WhenAll(tasks);
    
    // Assert
    Assert.That(callCount, Is.EqualTo(1), "Should execute only once");
    Assert.That(results, Is.All.EqualTo("result"));
}
```

## 🔍 Code Review Process

### **Before Submitting**
- [ ] All tests pass (`dotnet test`)
- [ ] Code follows style guidelines
- [ ] Added tests for new functionality
- [ ] Updated documentation if needed
- [ ] No breaking changes (or clearly documented)

### **Pull Request Guidelines**
- **Title**: Clear, descriptive summary
- **Description**: Explain what and why, not just how
- **Link Issues**: Reference related issues with `#123`
- **Size**: Keep PRs focused and reasonably sized
- **Tests**: Include test results if significant changes

### **Review Criteria**
We evaluate PRs based on:
- **Functionality**: Does it work as intended?
- **Code Quality**: Is it maintainable and readable?
- **Performance**: Any negative impact on performance?
- **Tests**: Adequate test coverage?
- **Documentation**: Updated appropriately?

## 🎯 Contribution Ideas

### **Good First Issues**
Perfect for newcomers:
- Documentation improvements
- Adding XML documentation
- Writing usage examples
- Fixing minor bugs

### **Intermediate**
For those comfortable with C#:
- Performance optimizations
- New cache features
- Enhanced error handling
- Better testing utilities

### **Advanced**
Complex contributions:
- Architectural improvements
- Distributed caching support
- Advanced concurrency patterns
- Integration with other libraries

## 📞 Getting Help

### **Questions?**
- 💬 **GitHub Discussions**: For general questions
- 🐛 **GitHub Issues**: For bugs and feature requests
- 📧 **Email**: `aburrio@gmail.com` for complex topics

### **Resources**
- [Architecture Overview](https://github.com/chanido/blitzcache/blob/master/IMPROVEMENTS.md)
- [Performance Benchmarks](https://github.com/chanido/blitzcache#performance)
- [Usage Examples](https://github.com/chanido/blitzcache/tree/master/BlitzCache.Tests/Examples)

## 🏆 Recognition

Contributors are recognized:
- **README**: Listed in contributors section
- **Release Notes**: Credited for significant contributions
- **Hall of Fame**: Special recognition for major contributions

## 📜 Code of Conduct

We're committed to providing a welcoming and inclusive experience for everyone. Please read our [Code of Conduct](CODE_OF_CONDUCT.md).

---

## 🙏 Thank You!

Every contribution makes BlitzCache better for the entire .NET community. Whether you're fixing a typo, adding a feature, or reporting a bug - **thank you for making BlitzCache awesome!** ⚡️

Happy coding! 🚀
