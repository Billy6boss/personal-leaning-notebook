# 12 Core OOP Concepts Every Software Developer Should Understand

When I first started learning programming, Object-Oriented Programming, or OOP, sounded like a very big and confusing topic.

Classes, objects, inheritance, polymorphism, abstraction — all these words looked heavy at the beginning.

But slowly I understood one simple thing:

OOP is not just about writing classes. It is a way to organize your code around real-world things. For example, in a real project, you may work with things like users, orders, payments, products, notifications, carts, invoices, and many more.

OOP helps you represent these things inside your code in a clean and structured way. Instead of keeping data in different variables and writing random functions everywhere, OOP allows you to keep related data and related actions together in one place.

This makes your code easier to understand, easier to update, easier to test, and easier to maintain when your project becomes bigger. But to use OOP properly, you need to understand its core concepts. Because OOP is not only about creating objects.

It is about learning how to model real problems, manage complexity, reduce repeated code, and avoid turning your codebase into a messy system that breaks every time you change something.

In this story, we will go through 12 core OOP concepts that every software developer should understand. I will explain each concept in simple words, with easy examples and code, so even if you are a beginner, you can follow along comfortably.

So, let’s get started.

💠 Core Building Blocks
🔹Classes
A class is one of the first things we learn in Object-Oriented Programming. In simple words, a class is like a plan or a blueprint. It tells us what kind of data an object will have and what actions that object can perform.

Press enter or click to view image in full size

For example, think about a car.

A car has some information:

brand
color
speed
And a car can do some actions:

start
stop
increase speed
So, if we want to represent a car in code, we can create a Car class. A class does not represent one real car directly. It only defines the structure. Later, we can create real objects from that class.

A simple real-life example is a house blueprint. A blueprint shows where the rooms, doors, windows, and walls should be. But you cannot live inside a blueprint. You need to build an actual house from it.

In the same way, a class is only a blueprint. To use it, we need to create objects from it.

Here is a simple Java example:

public class Car {
    private String brand;
    private String color;
    private int speed;
public Car(String brand, String color) {
        this.brand = brand;
        this.color = color;
        this.speed = 0;
    }
    public void start() {
        System.out.println(brand + " car has started.");
    }
    public void increaseSpeed(int value) {
        speed = speed + value;
        System.out.println("Current speed: " + speed + " km/h");
    }
    public String getDetails() {
        return brand + " - " + color;
    }
}
In this example, Car is a class. It contains data like brand, color, and speed. It also contains actions like start(), increaseSpeed(), and getDetails().

So instead of keeping car data in random variables and writing separate functions outside, we keep related data and related behavior together inside one class.

That is the main idea of a class. It gives structure to your code. But remember, a class by itself does not do much. It is just a template. To actually use it, we need to create objects from it. We will understand objects next.

🔹Objects
Now that we understand classes, let’s talk about objects. If a class is a blueprint, then an object is the real thing created from that blueprint. In simple words, an object is a real usable copy of a class. For example, we created a Car class in the previous section. That Car class only defines what a car should have and what a car can do.

Press enter or click to view image in full size

It says a car can have:

brand
color
speed
And it can do actions like:

start
stop
increase speed
But the class itself is not a real car. To use it, we need to create objects from it. Here is an example:

Car car1 = new Car("Toyota", "Red");
Car car2 = new Car("Honda", "Blue");
Car car3 = new Car("Tesla", "Black");
Here, car1, car2, and car3 are objects. All three objects are created from the same Car class. But each object has its own values.

car1 is a red Toyota.

car2 is a blue Honda.

car3 is a black Tesla.

Now we can use these objects like this:

car1.start();
car1.increaseSpeed(40);
car2.start();
car2.increaseSpeed(60);
car3.start();
car3.increaseSpeed(80);
Each object works independently.

If we increase the speed of car1, it will not change the speed of car2. If car2 starts, it does not mean car3 has also started.

They are separate objects created from the same class. This is one of the most important ideas in OOP. A class gives the structure. An object gives the real values.

Think about a house blueprint again. One blueprint can be used to build many houses. But after building them, each house can have a different color, different owner, different furniture, and different address. In the same way, one class can create many objects, and every object can have its own data.

So, classes and objects together help us keep related data and behavior in one clean place. But in bigger applications, sometimes we do not only want to create objects. We also want to define a common set of actions that different classes must follow. That is where interfaces come in.

🔹Interfaces
Now let’s understand interfaces. An interface is like a contract. It tells a class what methods it must have, but it does not explain how those methods should work. In simple words, an interface says:

“This class must provide these actions.”

But the class can decide its own way to perform those actions.

Let’s take a very simple example.

Imagine we are building a notification system. In an app, we may need to send notifications in different ways:

Email
SMS
WhatsApp
Push notification
Press enter or click to view image in full size

All of them are different, but they have one common job:

They send a message. So instead of writing different logic everywhere, we can create one common interface called NotificationService.

public interface NotificationService {
    void sendMessage(String user, String message);
}
This interface says:

Any class that wants to become a notification service must have a sendMessage() method. Now we can create different classes using this interface.

public class EmailNotification implements NotificationService {
@Override
    public void sendMessage(String user, String message) {
        System.out.println("Sending email to " + user + ": " + message);
    }
}
public class SMSNotification implements NotificationService {
@Override
    public void sendMessage(String user, String message) {
        System.out.println("Sending SMS to " + user + ": " + message);
    }
}
public class WhatsAppNotification implements NotificationService {
@Override
    public void sendMessage(String user, String message) {
        System.out.println("Sending WhatsApp message to " + user + ": " + message);
    }
}
Here, all three classes follow the same interface.

They all have the sendMessage() method. But each class has its own way of sending the message. Email sends an email. SMS sends a text message. WhatsApp sends a WhatsApp message. Now we can use the interface like this:

public class AlertManager {
    private NotificationService notificationService;
public AlertManager(NotificationService notificationService) {
        this.notificationService = notificationService;
    }
    public void sendAlert(String user) {
        notificationService.sendMessage(user, "Your order has been shipped.");
    }
}
Now AlertManager does not care whether the message is going through email, SMS, or WhatsApp.

It only knows one thing:

There is a NotificationService, and it can send a message. That is the power of an interface. Later, if we want to switch from email to SMS, we do not need to rewrite the AlertManager class. We only change the object we pass into it.

NotificationService email = new EmailNotification();
AlertManager alert1 = new AlertManager(email);
alert1.sendAlert("Shivam");
NotificationService sms = new SMSNotification();
AlertManager alert2 = new AlertManager(sms);
alert2.sendAlert("Rahul");
This makes our code flexible.

If tomorrow we want to add push notifications, we can simply create a new class:

public class PushNotification implements NotificationService {
@Override
    public void sendMessage(String user, String message) {
        System.out.println("Sending push notification to " + user + ": " + message);
    }
}
We do not need to change the old code. We just add a new class that follows the same interface. That is why interfaces are very useful in real projects. They help us write code that is easy to extend, easy to replace, and easy to maintain.

So remember this simple idea:

An interface defines what must be done. A class decides how it will be done. Interfaces tell us what behavior should exist. The next important step is understanding how to design these classes properly using the four main pillars of OOP.

💠 The 4 Pillars That Make OOP Powerful
🔹Encapsulation
Encapsulation is one of the most important pillars of OOP. In simple words, encapsulation means keeping data and methods together inside a class, and not allowing other parts of the program to directly change the internal data.

It is like putting important data inside a safe box. Other people can use the safe box through proper buttons or keys, but they cannot directly touch everything inside.

In programming, this helps us protect our object from invalid changes. Let’s understand this with a simple bank account example.

A bank account has some data:

account holder name
balance
And it has some actions:

deposit money
withdraw money
check balance
Now imagine if the balance is public and anyone can change it directly.

public class BankAccount {
    public String accountHolder;
    public double balance;
}
Now someone can do this:

BankAccount account = new BankAccount();
account.accountHolder = "Shivam";
account.balance = 5000;
account.balance = -10000; // Wrong value
This is a problem. A real bank account balance should not become negative like this without any proper rule. But because balance is public, anyone can change it directly. This makes the object unsafe. Now let’s use encapsulation.

public class BankAccount {
    private String accountHolder;
    private double balance;
public BankAccount(String accountHolder, double openingBalance) {
        this.accountHolder = accountHolder;
        if (openingBalance >= 0) {
            this.balance = openingBalance;
        } else {
            this.balance = 0;
        }
    }
    public void deposit(double amount) {
        if (amount > 0) {
            balance = balance + amount;
            System.out.println("Deposited: " + amount);
        } else {
            System.out.println("Deposit amount must be positive.");
        }
    }
    public void withdraw(double amount) {
        if (amount > 0 && amount <= balance) {
            balance = balance - amount;
            System.out.println("Withdrawn: " + amount);
        } else {
            System.out.println("Invalid withdrawal amount.");
        }
    }
    public double getBalance() {
        return balance;
    }
    public String getAccountHolder() {
        return accountHolder;
    }
}
Now the important data is private.

private double balance;
This means no one can directly change the balance from outside the class. They cannot do this anymore:

account.balance = -10000; // Not allowed
Instead, they have to use proper methods like:

BankAccount account = new BankAccount("Shivam", 5000);
account.deposit(2000);
account.withdraw(1000);
System.out.println(account.getBalance());
Now the class controls how the balance changes. If someone tries to deposit a negative amount, the class can reject it. If someone tries to withdraw more money than available, the class can stop it. That is the main idea of encapsulation. We hide the internal data and expose only safe methods to work with that data.

This makes our code more secure, more controlled, and easier to maintain. If tomorrow we want to add more rules, like minimum balance, transaction charges, or daily withdrawal limits, we can add them inside the BankAccount class.

The outside code does not need to know all these internal details. It only uses simple methods like deposit(), withdraw(), and getBalance().

So remember this simple line:

Encapsulation protects the data by controlling how it is accessed and changed. It hides the internal details of a class. But sometimes we do not only want to hide data. We also want to hide unnecessary complexity from the user. That idea is called abstraction, and we will understand it next.

🔹Abstraction
Abstraction is another important pillar of OOP. In simple words, abstraction means hiding unnecessary details and showing only what is important. It helps us use something without knowing all the complex work happening behind the scenes.

Let’s take a real-world example.

Think about using an ATM machine. When you want to withdraw money, you only do a few simple steps:

insert your card
enter your PIN
choose withdraw option
enter amount
collect cash
That’s it.

But behind the scenes, many things happen. The ATM checks your card, verifies your PIN, talks to the bank server, checks your balance, updates your account, prints a receipt, and then gives you cash.

As a user, you do not need to understand all this internal logic. You only see a simple interface. That is abstraction. It hides the complicated process and gives you a simple way to use it. In programming, abstraction works in the same way. We create simple methods for the outside world, and we hide the complex logic inside the class.

Let’s understand this with a simple FoodOrder example. Imagine we are building a food delivery app. The user only wants to place an order. They do not care about every internal step like checking restaurant availability, calculating delivery charges, applying discount, assigning delivery partner, and sending confirmation. So we can create a simple method called placeOrder().

public class FoodOrder {
public void placeOrder(String foodItem, String userLocation) {
        checkRestaurantAvailability(foodItem);
        calculateDeliveryCharge(userLocation);
        applyDiscount();
        assignDeliveryPartner();
        sendConfirmation();
        System.out.println("Your order for " + foodItem + " has been placed successfully.");
    }
    private void checkRestaurantAvailability(String foodItem) {
        System.out.println("Checking restaurant availability for " + foodItem);
    }
    private void calculateDeliveryCharge(String userLocation) {
        System.out.println("Calculating delivery charge for " + userLocation);
    }
    private void applyDiscount() {
        System.out.println("Applying available discount.");
    }
    private void assignDeliveryPartner() {
        System.out.println("Assigning delivery partner.");
    }
    private void sendConfirmation() {
        System.out.println("Sending order confirmation.");
    }
}
Now the outside code can use it like this:

FoodOrder order = new FoodOrder();
order.placeOrder("Pizza", "Delhi");
The user of this class only calls one simple method:

placeOrder()
They do not need to call all the small internal methods one by one.

They do not need to know how the restaurant is checked.

They do not need to know how delivery charge is calculated.

They do not need to know how the delivery partner is assigned.

All those details are hidden inside the class. This is abstraction. We expose only the important action and hide the complex steps behind it. This makes the code easier to use. It also makes the code easier to change. For example, tomorrow if we want to change how delivery partners are assigned, we can update the internal assignDeliveryPartner() method.

The outside code will still call the same placeOrder() method. Nothing changes for the user of the class. That is the beauty of abstraction. It gives a simple outside view and hides the complex inside work.

So remember this simple line:

Abstraction hides complexity and shows only what is necessary. Encapsulation protects the internal data. Abstraction hides the internal process. Both are related, but they solve different problems. Now, what if multiple classes share the same data and behavior? That is where inheritance comes in.

🔹Inheritance
Inheritance is another important concept in OOP. In simple words, inheritance means one class can take properties and methods from another class. The class that gives the common code is called the parent class. The class that receives that code is called the child class. This helps us avoid writing the same code again and again.

Let’s understand this with a simple real-world example.

Think about different types of employees in a company. A company can have:

full-time employees
part-time employees
interns
All of them are employees.

So they may have some common details:

name
employee ID
department
And they may have some common actions:

show employee details
calculate salary
Instead of writing these common things again in every class, we can create one parent class called Employee. Then other classes can inherit from it.

Here is a simple Java example:

public class Employee {
    protected String name;
    protected String employeeId;
    protected String department;
public Employee(String name, String employeeId, String department) {
        this.name = name;
        this.employeeId = employeeId;
        this.department = department;
    }
    public void showDetails() {
        System.out.println("Name: " + name);
        System.out.println("Employee ID: " + employeeId);
        System.out.println("Department: " + department);
    }
}
Here, Employee is the parent class. It contains common data and common behavior. Now we can create a child class called FullTimeEmployee.

public class FullTimeEmployee extends Employee {
    private double monthlySalary;
public FullTimeEmployee(String name, String employeeId, String department, double monthlySalary) {
        super(name, employeeId, department);
        this.monthlySalary = monthlySalary;
    }
    public void calculateSalary() {
        System.out.println("Monthly Salary: " + monthlySalary);
    }
}
Now let’s create another child class called Intern.

public class Intern extends Employee {
    private double stipend;
public Intern(String name, String employeeId, String department, double stipend) {
        super(name, employeeId, department);
        this.stipend = stipend;
    }
    public void calculateStipend() {
        System.out.println("Monthly Stipend: " + stipend);
    }
}
Now both FullTimeEmployee and Intern get common details from the Employee class.

They do not need to write name, employeeId, department, and showDetails() again.

We can use them like this:

FullTimeEmployee emp1 = new FullTimeEmployee(
    "Shivam",
    "EMP101",
    "Engineering",
    60000
);
emp1.showDetails();
emp1.calculateSalary();
Intern intern1 = new Intern(
    "Rahul",
    "INT201",
    "Development",
    15000
);
intern1.showDetails();
intern1.calculateStipend();
Here, both objects can use showDetails() because that method comes from the parent Employee class.

But they also have their own special behavior. A full-time employee has a salary. An intern has a stipend. That is the main idea of inheritance.

It helps child classes reuse common code from a parent class and still add their own specific behavior.

But inheritance should be used carefully. Use inheritance only when there is a clear “is-a” relationship. For example:

A full-time employee is an employee.

An intern is an employee.

A car is a vehicle.

A dog is an animal.

These are natural relationships. But do not use inheritance only because you want to reuse some code. If the relationship does not feel natural, inheritance can make the code confusing. In that case, composition is usually a better option.

So remember this simple line:

Inheritance allows one class to reuse and extend the behavior of another class. It helps us reduce repeated code when classes share a real parent-child relationship. But what happens when different child classes have the same method name, but each one behaves differently? That idea is called polymorphism, and we will understand it next.

🔹Polymorphism
Polymorphism is one of those words that sounds difficult at first. But the idea is actually simple. Polymorphism means many forms. In OOP, it means the same action can behave differently depending on the object that is using it. Let’s understand this with a real-world example.

Think about a payment system. In an app, users may pay using different methods:

credit card
UPI
PayPal
All payment methods have one common action:

pay()
But each payment method performs that action in its own way. Credit card payment uses card details. UPI payment uses a UPI ID. PayPal payment uses a PayPal account. The method name is the same, but the behavior is different. That is polymorphism.

There are mainly two types of polymorphism:

Compile-time polymorphism
This happens when we use the same method name with different parameters. This is also called method overloading.

Runtime polymorphism
This happens when child classes use the same method name but provide their own implementation. This is also called method overriding.

Runtime polymorphism is more commonly used in real projects. Let’s understand it with a simple example.

First, we create a common interface:

public interface PaymentMethod {
    void pay(double amount);
}
This interface says:

Any payment method must have a pay() method. Now we can create different payment classes.

public class CreditCardPayment implements PaymentMethod {
@Override
    public void pay(double amount) {
        System.out.println("Paid ₹" + amount + " using Credit Card.");
    }
}
public class UpiPayment implements PaymentMethod {
@Override
    public void pay(double amount) {
        System.out.println("Paid ₹" + amount + " using UPI.");
    }
}
public class PayPalPayment implements PaymentMethod {
@Override
    public void pay(double amount) {
        System.out.println("Paid ₹" + amount + " using PayPal.");
    }
}
Here, all three classes follow the same interface.

They all have the same method:

pay()
But each class handles payment in a different way.

Now look at this code:

import java.util.List;
public class Checkout {
    public static void main(String[] args) {
        List<PaymentMethod> paymentMethods = List.of(
            new CreditCardPayment(),
            new UpiPayment(),
            new PayPalPayment()
        );
        for (PaymentMethod paymentMethod : paymentMethods) {
            paymentMethod.pay(1000);
        }
    }
}
In this loop, we are not checking whether the object is a credit card payment, UPI payment, or PayPal payment.

We simply call:

paymentMethod.pay(1000);
And Java automatically runs the correct version of the pay() method based on the actual object.

If the object is CreditCardPayment, it runs the credit card logic. If the object is UpiPayment, it runs the UPI logic. If the object is PayPalPayment, it runs the PayPal logic.

That is the power of polymorphism. The same method call can produce different behavior depending on the object. Now imagine tomorrow we want to add a new payment method, like wallet payment.

We can simply create a new class:

public class WalletPayment implements PaymentMethod {
@Override
    public void pay(double amount) {
        System.out.println("Paid ₹" + amount + " using Wallet.");
    }
}
We do not need to rewrite the whole checkout logic. We just add the new class and use it. This makes our code flexible and easy to extend.

So remember this simple line:

Polymorphism allows different objects to respond to the same method in their own way. It helps us write code that works with a common interface, while each class handles the actual behavior differently. Now that we understand how individual classes are structured and designed, let’s move one step further and understand how objects connect with each other.

💠 Object Relationships: How Classes Work Together
🔹Association
Now let’s understand how objects are connected with each other. The first relationship is called association. In simple words, association means one object knows about another object. Both objects are connected, but they can still exist independently. One object does not fully own the other object.

Press enter or click to view image in full size

Let’s take a simple real-world example.

Think about a teacher and a student. A teacher can teach many students. A student can learn from many teachers. But both can exist separately. If one student leaves the class, the teacher still exists. If one teacher leaves the school, the student can still learn from another teacher. So the relationship is there, but ownership is not strong.

That is association.

Let’s see this in Java:

import java.util.ArrayList;
import java.util.List;
public class Teacher {
    private String name;
    private List<Student> students;
    public Teacher(String name) {
        this.name = name;
        this.students = new ArrayList<>();
    }
    public void addStudent(Student student) {
        students.add(student);
    }
    public void showStudents() {
        System.out.println(name + " teaches:");
        for (Student student : students) {
            System.out.println(student.getName());
        }
    }
}
Now we create a Student class:

public class Student {
    private String name;
public Student(String name) {
        this.name = name;
    }
    public String getName() {
        return name;
    }
}
Now we can connect teacher and student objects:

public class Main {
    public static void main(String[] args) {
        Teacher teacher = new Teacher("Mr. Sharma");
Student student1 = new Student("Aman");
        Student student2 = new Student("Priya");
        teacher.addStudent(student1);
        teacher.addStudent(student2);
        teacher.showStudents();
    }
}
Here, Teacher and Student are two separate objects. The teacher object knows about the student objects. But the students are not completely owned by the teacher. They are created separately. That means they can exist even outside the Teacher class. This is the main point of association. Objects are connected, but they are still independent.

You can think of association like this:

A teacher teaches students.
A doctor treats patients.
A driver drives a car.
A customer places an order.
In all these examples, objects are related, but one object does not fully control the life of the other object.

So remember this simple line:

Association means one object is connected to another object, but both can live independently. Association is the most general relationship between objects. But sometimes, one object is not just connected to another object. Sometimes, one object is a part of another object, but it can still exist separately. That relationship is called aggregation.

🔹Aggregation
Aggregation is a special type of association. In simple words, aggregation means one object has another object, but the child object can still live independently. It is a has-a relationship, but not a very strict one.

Press enter or click to view image in full size

Let’s understand this with a simple real-world example.

Think about a school and teachers. A school has many teachers. But teachers can exist without that school. If a school closes, the teachers do not disappear. They can join another school. So the school has teachers, but it does not fully control the life of the teachers. That is aggregation.

Let’s see this in Java:

import java.util.ArrayList;
import java.util.List;
public class School {
    private String name;
    private List<Teacher> teachers;
    public School(String name) {
        this.name = name;
        this.teachers = new ArrayList<>();
    }
    public void addTeacher(Teacher teacher) {
        teachers.add(teacher);
    }
    public void showTeachers() {
        System.out.println("Teachers in " + name + ":");
        for (Teacher teacher : teachers) {
            System.out.println(teacher.getName());
        }
    }
}
Now we create a Teacher class:

public class Teacher {
    private String name;
    private String subject;
public Teacher(String name, String subject) {
        this.name = name;
        this.subject = subject;
    }
    public String getName() {
        return name + " teaches " + subject;
    }
}
Now let’s use both classes:

public class Main {
    public static void main(String[] args) {
        Teacher teacher1 = new Teacher("Mr. Sharma", "Math");
        Teacher teacher2 = new Teacher("Ms. Verma", "Science");
School school = new School("Green Valley School");
        school.addTeacher(teacher1);
        school.addTeacher(teacher2);
        school.showTeachers();
    }
}
Here, the Teacher objects are created outside the School class. After that, we add them to the school. This is important. The school is not creating the teachers internally. The teachers already exist, and the school is only keeping a reference to them. That means teachers can still exist even if the school object is removed. For example, the same teacher can join another school:

School anotherSchool = new School("Sunrise Public School");
anotherSchool.addTeacher(teacher1);
Here, teacher1 can be reused in another school. This shows that the teacher has its own independent life. That is the main idea of aggregation. The whole object has parts, but the parts can survive without the whole.

Some more examples:

A department has employees.
A library has books.
A team has players.
A school has teachers.
In all these examples, the smaller objects can still exist even if the bigger object is removed.

So remember this simple line:

Aggregation means one object has another object, but the child object can still exist independently. In association, objects are just connected. In aggregation, one object has another object. But the ownership is still weak. Now what if the child object cannot exist without the parent object? That stronger relationship is called composition.

🔹Dependency
Dependency is another type of relationship between classes. In simple words, dependency means one class uses another class for a short time. It is a temporary relationship. One class does not own the other class. It also does not keep the other class permanently inside itself. It only uses it when needed. That is why dependency is considered the weakest relationship between classes.

Press enter or click to view image in full size

Let’s understand this with a simple real-world example.

Imagine you are ordering food online. The FoodOrderService needs to send a confirmation message after the order is placed. For that, it can use a MessageSender. But the food order service does not own the message sender forever. It only uses it during the order process. Once the message is sent, the work is done. That is dependency.

Here is a simple Java example:

public class MessageSender {
public void sendMessage(String phoneNumber, String message) {
        System.out.println("Sending message to " + phoneNumber + ": " + message);
    }
}
Now we create a FoodOrderService class.

public class FoodOrderService {
public void placeOrder(String foodItem, String phoneNumber, MessageSender messageSender) {
        System.out.println("Order placed for: " + foodItem);
        messageSender.sendMessage(
            phoneNumber,
            "Your order for " + foodItem + " has been placed successfully."
        );
    }
}
Now let’s use it:

public class Main {
    public static void main(String[] args) {
        FoodOrderService orderService = new FoodOrderService();
        MessageSender messageSender = new MessageSender();
orderService.placeOrder("Pizza", "9876543210", messageSender);
    }
}
Here, FoodOrderService depends on MessageSender. But notice one important thing. FoodOrderService is not storing MessageSender as a field. It is only using it as a method parameter:

placeOrder(String foodItem, String phoneNumber, MessageSender messageSender)
That means the relationship exists only while the placeOrder() method is running. After the method finishes, the relationship is gone. This is dependency. The class uses another class to complete some work, but it does not control its lifetime.

Some more simple examples:

A report generator uses a printer.
A checkout service uses a discount calculator.
A controller uses a validation service.
A file uploader uses a file compressor.
In all these cases, one class uses another class for a specific task.

So remember this simple line:

Dependency means one class temporarily uses another class to complete some work. It is a “uses-a” relationship. Dependency is weaker than association, aggregation, and composition because the object is not stored for a long time. It is just used when needed. Now we have seen how objects can connect with each other in different ways. The final concept brings us back to interfaces and shows how classes actually follow the contracts they promise.

🔹 Realization
Realization is the final concept in this list. The word may sound a little technical, but the idea is simple. Realization is the relationship between an interface and the class that implements that interface. In simple words:

An interface defines a contract. A class follows that contract and provides the real working code. That relationship is called realization. Let’s understand this with a real-world example. Imagine a delivery app.

The app supports different delivery partners:

Bike delivery
Car delivery
Drone delivery
All of them must do one common job:

Deliver the order. So first, we create an interface called DeliveryPartner.

public interface DeliveryPartner {
    void deliverOrder(String orderId, String address);
}
This interface says:

Any delivery partner must have a deliverOrder() method. But the interface does not say how the delivery will happen. Now different classes can implement this interface in their own way.

public class BikeDelivery implements DeliveryPartner {
@Override
    public void deliverOrder(String orderId, String address) {
        System.out.println("Delivering order " + orderId + " by bike to " + address);
    }
}
public class CarDelivery implements DeliveryPartner {
@Override
    public void deliverOrder(String orderId, String address) {
        System.out.println("Delivering order " + orderId + " by car to " + address);
    }
}
public class DroneDelivery implements DeliveryPartner {
@Override
    public void deliverOrder(String orderId, String address) {
        System.out.println("Delivering order " + orderId + " by drone to " + address);
    }
}
Here, BikeDelivery, CarDelivery, and DroneDelivery are realizing the DeliveryPartner interface. That means they are accepting the contract and giving real implementation for it. The interface only says what should be done. The class decides how it should be done. Now we can use it like this:

public class DeliveryService {
    private DeliveryPartner deliveryPartner;
public DeliveryService(DeliveryPartner deliveryPartner) {
        this.deliveryPartner = deliveryPartner;
    }
    public void shipOrder(String orderId, String address) {
        deliveryPartner.deliverOrder(orderId, address);
    }
}
Now DeliveryService does not care whether the order is delivered by bike, car, or drone.

It only depends on the interface:

DeliveryPartner
This makes the code flexible.

DeliveryPartner partner = new BikeDelivery();
DeliveryService service = new DeliveryService(partner);
service.shipOrder("ORD101", "Delhi");
Tomorrow, if we want to add a new delivery method, like RobotDelivery, we do not need to change the main delivery service. We only create a new class and implement the same interface.

public class RobotDelivery implements DeliveryPartner {
@Override
    public void deliverOrder(String orderId, String address) {
        System.out.println("Delivering order " + orderId + " by robot to " + address);
    }
}
That is the power of realization. It connects the abstract idea with the real implementation. The interface is the promise. The class is the actual work.

So remember this simple line:

Realization means a class implements an interface and provides real code for the methods promised by that interface. This is also what makes polymorphism possible. Because when many classes implement the same interface, we can use them through one common type and let each class behave in its own way.

So realization is the bridge between a contract and the actual behavior.

Final Thoughts
Once these ideas become clear, you will start seeing them everywhere: in backend systems, mobile apps, design patterns, frameworks, and even Low-Level Design interviews.

So take your time with these concepts. Practice them with small examples. Try to connect them with real-world scenarios. That is how OOP slowly starts making sense.

Thank you for reading. 🥰

If you found this story helpful, please hit the like button, repost it, and follow me for more beginner-friendly software development content every week.

And if you have any questions, suggestions, or your own way of understanding these concepts, feel free to share them in the comments.

See you in the next awesome story. 🫡