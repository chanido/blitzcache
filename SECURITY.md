# Security Policy

## Supported Versions

We actively support the following versions of BlitzCache with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 2.x.x   | ✅ Full support    |
| < 2.0   | ❌ End of life     |

## Reporting Security Vulnerabilities

We take security seriously and appreciate your help in making BlitzCache safe for everyone.

### How to Report

**DO NOT** open public GitHub issues for security vulnerabilities.

Instead, please report security issues privately:

1. **Email**: Send details to `aburrio@gmail.com`
2. **Subject Line**: `[SECURITY] BlitzCache Vulnerability Report`
3. **Include**:
   - Description of the vulnerability
   - Steps to reproduce the issue
   - Potential impact assessment
   - Any suggested fixes (if available)

### What to Expect

- **Acknowledgment**: Within 48 hours
- **Initial Assessment**: Within 1 week
- **Regular Updates**: Every week until resolution
- **Public Disclosure**: Coordinated after fix is available

### Security Considerations

BlitzCache is designed with security in mind:

#### ✅ **Safe by Design**
- **No External Dependencies**: Minimal attack surface
- **Memory Safe**: No buffer overflows or memory corruption
- **Thread Safe**: Protected against race conditions
- **Input Validation**: Cache keys are properly validated

#### ✅ **Cache Security**
- **Isolation**: Each cache instance is isolated
- **No Data Leakage**: Proper cleanup prevents data bleeding
- **Key Collision Protection**: Robust key handling prevents collisions

#### ⚠️ **Security Considerations**
- **Cache Keys**: Use appropriate cache keys to prevent information disclosure
- **Sensitive Data**: Consider encryption for sensitive cached data
- **Memory**: Cached data remains in memory until expiration/eviction

### Best Practices

When using BlitzCache in production:

1. **Sanitize Cache Keys**: Ensure cache keys don't contain sensitive information
2. **Monitor Memory Usage**: Set appropriate expiration times for large objects
3. **Validate Inputs**: Always validate data before caching
4. **Use HTTPS**: Ensure secure transport when caching API responses
5. **Regular Updates**: Keep BlitzCache updated to the latest version

### Security Updates

Security updates will be:
- **Prioritized**: Released as soon as possible
- **Documented**: Detailed in CHANGELOG.md
- **Communicated**: Announced via GitHub releases
- **Backwards Compatible**: When possible, maintained API compatibility

### Hall of Fame

We recognize security researchers who help improve BlitzCache:

- *Your name could be here!*

Thank you for helping keep BlitzCache secure! 🛡️
