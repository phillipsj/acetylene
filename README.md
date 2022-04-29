# Acetylene
A Windows init system based on ignition and combustion for Humans.

Workign with unattend.xml files is challenging and doesn't always cover an entire use case. Often building a Windows image with a tool like Packer is part of a solution. The last step is then initializing the produced image configuring user accounts, hostname, etc. There are limited solutions to solve this problem and why Acetylene will exist.

The goal of Acetylene is to provide a system, written in Rust, for Windows that can consume a standard ignition file and even support the combustion concepts. The combustion component will just use PowerShell instead of Bash since it's Windows.
