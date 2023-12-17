# GravyNetworking
A multi threaded networking library on top of [ENet](https://github.com/nxrighthere/ENet-CSharp). The idea is to have a simple server and client library that require little setup. Both libraries are callback based and structured in a similar manner. Your only responsibility is to handle incoming connections/packets however you like, and implement whatever logic is required to make the experience pleasant for the user.

# Installation
To get the server library:
```
dotnet add package JAJ.Packages.GravyNetworking.Server --version 1.1.1
```

For the client library:
```
dotnet add package JAJ.Packages.GravyNetworking.Client --version 1.1.1
```

# Example
See [here](https://github.com/japajoe/GravyNetworking/tree/main/Example).
