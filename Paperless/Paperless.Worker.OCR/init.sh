#!/bin/sh

# Run only once ? detect marker file
if [ -f /tmp/symlink_created ]; then
    exit 0
fi

# Create symlinks
ln -sf /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so
ln -sf /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so

# Mark successful run
touch /tmp/symlink_created

exit 0
