Stage 2: Understand Object-Oriented Design
Knowing syntax is not the same as knowing how to design software.

You may know how to create a class while still being unsure what responsibilities that class should have.

Learn:

Encapsulation
Abstraction
Inheritance
Polymorphism
Composition
Interfaces
Abstract classes
SOLID principles
Dependency injection
You should be able to explain why composition is often safer than deep inheritance.

You should also understand that applying every design principle everywhere does not automatically produce good software. Principles exist to help you make better decisions, not to replace judgment.

What to Build
Take one of your console applications and refactor it:

Separate business rules from input and output
Introduce interfaces around external dependencies
Replace large methods with focused services
Add validation
Add automated tests
For example, an expense tracker should not contain file-writing logic, user-input handling, calculations, and validation inside one enormous method.

Separate those responsibilities and observe how the design becomes easier to test and modify.

Refactoring teaches design more honestly than memorizing definitions.