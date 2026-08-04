# What Happens When Multiple Users Hit Your API at the Same Time?

If 1,000 users click the “Buy Now” button simultaneously, does Spring Boot create 1,000 copies of your application? Or does one server magically handle everyone at once?

If you’ve ever built a Spring Boot REST API, you’ve probably tested it like this:

@GetMapping("/hello")
public String hello() {
    return "Hello World";
}
You start your application, open Postman, hit the endpoint, and everything works perfectly.

But here’s the real question:

What happens when 10 users hit your API simultaneously?
What about 100?
Or 10,000?

Does Spring Boot queue every request?
Does it create a new application instance?
How does it avoid mixing up user data?

Let’s explore what actually happens behind the scenes.

The Journey Begins
Imagine your Spring Boot application is running on port 8080.

http://localhost:8080/api/products
Suddenly, five users send requests at exactly the same moment.

User A ---> API
User B ---> API
User C ---> API
User D ---> API
User E ---> API
At first glance, it looks impossible.

How can one application serve five users simultaneously?

The answer lies in threads.

Meet Tomcat
By default, Spring Boot comes with an embedded Tomcat server.

When your application starts, Tomcat creates a pool of worker threads.

Think of it as a restaurant.

Customers = HTTP requests
Waiters = Threads
Kitchen = Your application
When customers arrive:

Customer 1 -> Waiter 1
Customer 2 -> Waiter 2
Customer 3 -> Waiter 3
Similarly:

Request A -> Thread 1
Request B -> Thread 2
Request C -> Thread 3
Each request gets its own thread.

What Is a Thread?
A thread is simply an independent path of execution.

Suppose this controller exists:

@GetMapping("/welcome")
public String welcome() {
    return Thread.currentThread().getName();
}
If multiple users call this endpoint, they might receive:

http-nio-8080-exec-1
http-nio-8080-exec-2
http-nio-8080-exec-3
http-nio-8080-exec-4
Each request runs on a different thread.

That’s how Spring Boot handles many users at once.

Does Spring Create Multiple Controllers?
This surprises many developers.

Suppose we have:

@RestController
public class UserController {

    @GetMapping("/users")
    public String users() {
        return "Users";
    }
}
Even if 1,000 users hit this endpoint,

Spring creates only ONE instance of UserController.

               UserController
                     |
       ------------------------------
       |      |      |      |      |
     T1      T2     T3     T4     T5
Multiple threads use the same controller object.

The same applies to:

Services
Repositories
Components
By default, Spring beans are Singleton scoped.

Why Singleton Beans Can Be Dangerous
Consider this service:

@Service
public class CounterService {

    private int counter = 0;
    public int increment() {
        return ++counter;
    }
}
Now imagine:

Thread 1 reads counter = 10
Thread 2 reads counter = 10
Thread 1 increments to 11
Thread 2 increments to 11
Expected:

11
12
Actual:

11
11
One increment is lost.

This is called a race condition.

The Golden Rule
Singleton beans should be stateless.

Good:

@Service
public class CalculatorService {

    public int add(int a, int b) {
        return a + b;
    }
}
Bad:

@Service
public class UserService {

    private String currentUser;

}
Never store request-specific data in singleton beans.

What Happens to Local Variables?
Suppose:

@GetMapping("/sum")
public int sum() {

    int a = 10;
    int b = 20;
    return a + b;
}
Even though multiple threads execute this method,

each thread gets its own copy of local variables.

Thread 1:
a=10
b=20
Thread 2:
a=10
b=20
Thread 3:
a=10
b=20
Local variables are thread-safe.

Instance variables are shared.

What If a Database Query Takes Time?
Imagine:

@GetMapping("/products")
public List<Product> products() {
    return repository.findAll();
}
The thread sends a SQL query.

While waiting,

that thread remains busy.

If many slow requests pile up,

eventually all Tomcat threads become occupied.

New requests must wait.

Tomcat Uses a Thread Pool
Spring Boot doesn’t create unlimited threads.

It maintains a thread pool.

A simplified example:

Pool Size = 200
Request 1 -> Thread 1
Request 2 -> Thread 2
...
Request 200 -> Thread 200
Request 201 -> Wait
If every thread is busy,

new requests are queued until a worker becomes available.

Why Slow APIs Hurt Performance
Suppose every request takes:

5 seconds
200 threads can process:

200 requests
After 5 seconds:
Next 200 requests
If your API responds in:

100 milliseconds
Those same threads can serve thousands of users per second.

Fast code means better scalability.

What About Async Processing?
Some tasks don’t need an immediate response.

For example:

Sending emails
PDF generation
Notifications
Report creation
Instead of blocking the request thread:

@Async
public void sendEmail() {
}
Spring executes the task in another thread pool.

The user gets a response faster.

What About Multiple CPUs?
Modern servers have multiple processor cores.

CPU Core 1
CPU Core 2
CPU Core 3
CPU Core 4
The operating system schedules different threads across these cores.

This allows true parallel execution.

A multicore machine can process many requests simultaneously.

How Does Spring Keep User Data Separate?
Suppose:

Alice logs in.
Bob logs in.
Charlie logs in.
Even though they use the same controller and service objects,

their request data stays isolated because each request has its own:

Thread
Stack memory
Method parameters
Local variables
That’s why Alice’s data doesn’t suddenly appear in Bob’s response.

A Simplified Request Flow
Here’s what happens when five users hit your API:

                 Internet
                    |
          ---------------------------
          |    |    |    |    |
          A    B    C    D    E
          |    |    |    |    |
          ---------------------------
                    |
               Embedded Tomcat
                    |
          ---------------------------
          |    |    |    |    |
         T1   T2   T3   T4   T5
          |    |    |    |    |
          ---------------------------
                    |
          DispatcherServlet
                    |
               Controller
                    |
                 Service
                    |
               Repository
                    |
                Database
                    |
                Response
Every request gets its own thread while sharing the same Spring beans.

Common Mistakes That Break Concurrency
❌ Storing user data in singleton beans
private String username;
❌ Using mutable shared collections without synchronization
private List<String> users = new ArrayList<>();
❌ Long-running blocking operations
Thread.sleep(10000);
❌ Heavy synchronous file operations.
Best Practices
✅ Keep services stateless.

✅ Use constructor injection.

✅ Prefer local variables over shared fields.

✅ Keep API responses fast.

✅ Use connection pooling.

✅ Use asynchronous processing for long-running tasks.

✅ Understand that singleton beans are shared across threads.

Final Thoughts
When multiple users hit your Spring Boot API at the same time, Spring doesn’t create multiple copies of your application.

Instead:

Embedded Tomcat receives the requests.
Each request gets a worker thread.
The same singleton beans handle many threads simultaneously.
Local variables stay isolated.
Shared mutable state can cause race conditions.
Performance depends on how quickly those threads finish their work.
The next time you deploy a Spring Boot application, remember:

Your API isn’t serving one user at a time. It’s a carefully orchestrated system of threads, shared beans, and thread pools working together to handle thousands of requests concurrently.

And that’s one of the reasons Spring Boot scales so well.