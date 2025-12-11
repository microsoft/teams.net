using System.Diagnostics.CodeAnalysis;

// Global suppressions for AOT compatibility issues in third-party libraries

// Suppress configuration binding source generation AOT warnings for Microsoft.Identity
[assembly: SuppressMessage("AOT", "SYSLIB1100", Justification = "Microsoft.Identity configuration binding source generation may not be fully AOT compatible but runtime support exists")]
[assembly: SuppressMessage("AOT", "SYSLIB1101", Justification = "Microsoft.Identity configuration binding source generation may not be fully AOT compatible but runtime support exists")]

// Suppress obsolete cryptography API warnings from Microsoft.Identity configuration binding source generator
[assembly: SuppressMessage("Reliability", "SYSLIB0026", Justification = "Microsoft.Identity configuration binding may use obsolete X509Certificate APIs but provides AOT alternatives")]
[assembly: SuppressMessage("Reliability", "SYSLIB0027", Justification = "Microsoft.Identity configuration binding may use obsolete cryptography APIs but provides AOT alternatives")]
[assembly: SuppressMessage("Reliability", "SYSLIB0028", Justification = "Microsoft.Identity configuration binding may use obsolete X509Certificate APIs but provides AOT alternatives")]