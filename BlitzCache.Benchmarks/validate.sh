#!/bin/bash
# Validation script for BlitzCache.Benchmarks

echo "🔍 Validating BlitzCache.Benchmarks..."

# Build in release mode
echo "📦 Building in Release mode..."
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

# Test basic functionality
echo "🧪 Testing basic functionality..."
dotnet run --no-build -c Release | grep -q "All cache libraries are working correctly!"
if [ $? -ne 0 ]; then
    echo "❌ Basic functionality test failed"
    exit 1
fi

echo "✅ All validations passed!"
echo "📊 To run full benchmarks: dotnet run -c Release"