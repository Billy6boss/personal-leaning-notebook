# Factory Pattern (Part 2): Step-by-Step Guide to Implementing a Simple DI Container

Its core function is simple: wrap the complex process of object creation, so that people who use the object don’t have to worry about it, completely separate creation from use, and the code can be much cleaner.

As for what counts as a “troublesome task”, we also mentioned two situations in the previous lesson: one is that when creating an object, you have to write a lot of if-else judgments to create different objects according to different conditions; the other is that when creating an object, you first have to assemble other objects it depends on, and also do a lot of initialization operations with many steps.

Today, we are going to talk about a more “troublesome” object creation scenario — the Dependency Injection Container, which is what we often call the DI Container. Many people deal with it every day when using Spring, but they only know how to use it but not why it works.

Today, we will completely take off its veil and figure out three key questions: What is the relationship between the DI Container and the Factory Pattern we talked about earlier? What are the core things a DI Container must be able to do? And most importantly, can we implement a simple DI Container ourselves?

No empty talk, let’s get straight to the point!

What’s the Difference Between Factory Pattern and DI Container?
Let me tell you the truth first: the DI Container is essentially based on the Factory Pattern, and it is essentially a “super large factory”. But it is much more powerful than the small factories we wrote in the previous lesson.

The factories we wrote in the previous lesson, whether simple factories or factory methods, are only responsible for creating a certain type or a group of related objects — for example, a factory specially created for parsers only takes care of parsers and nothing else. But the DI Container is different. It has to take care of the creation of all objects in the entire application, equivalent to an “object housekeeper” that manages all objects.

Moreover, this “housekeeper” does more than just create objects. For example, it reads configuration files to know which objects to create and what each object depends on; it also manages the “lifetime” of objects — when to create them, when to destroy them, and whether to reuse them, it can handle all these.

Next, let’s first talk about the core capabilities that a usable simple DI Container must have.

A Simple DI Container Only Needs to Do Three Core Things
No need to remember those complex concepts. A usable simple DI Container actually does three core things: read configuration, create objects, and manage objects. Let’s talk about them one by one, all in plain language.

1. Read Configuration: Know Which Objects to Create
A big problem with the factories we wrote in the previous lesson is that which objects to create are all hard-coded in the code in advance. For example, a factory that creates parsers has hard-coded in the code that it can only create two types of parsers: json and xml. To add a new one, you have to modify the factory code.

But the DI Container is a general framework. It doesn’t know which objects are in the application you are developing, so it can’t hard-code all possible objects into the framework, right? Therefore, we have to tell the DI Container through configuration files: which objects I want to create, how to create each object, and what they depend on.

Let’s take the most familiar example to everyone: Spring’s xml configuration. We don’t need to make it complicated, just write a simple configuration file to tell the container to create two objects: User Service (UserService) and Database Connection (DbConnection), and UserService can only be created if it depends on DbConnection.

First, let’s look at the two classes we want to create, which are very simple and close to actual development scenarios:

// User Service class, which depends on DbConnection to work
public class UserService {
    private DbConnection dbConnection;


// Constructor injection of dependencies: To create UserService, DbConnection must be passed in
    public UserService(DbConnection dbConnection) {
        this.dbConnection = dbConnection;
    }
    // A simple business method to test if it can be used normally
    public void queryUser() {
        System.out.println("Query user information, using database address: " + dbConnection.getUrl());
    }
    //... Other business methods are omitted
}
// Database connection class, which needs to configure IP and port
public class DbConnection {
    private String url;
    private String username;
    private String password;
    // Parameterized constructor, initialization requires configuration information
    public DbConnection(String url, String username, String password) {
        this.url = url;
        this.username = username;
        this.password = password;
    }
    // Getter method for UserService to use
    public String getUrl() {
        return url;
    }
    //... Other getters/setters are omitted
}
Then there is the configuration file beans.xml. We write all the objects to be created and their dependency relationships here:

<beans>
    <!-- Configure the database connection object and specify constructor parameters -->
    <bean id="dbConnection" class="com.xzg.DbConnection">
        <constructor-arg type="String" value="jdbc:mysql://127.0.0.1:3306/test"/>
        <constructor-arg type="String" value="root"/>
        <constructor-arg type="String" value="123456"/>
    </bean>


<!-- Configure the user service object, which depends on the dbConnection above -->
    <bean id="userService" class="com.xzg.UserService">
        <constructor-arg ref="dbConnection"/> <!-- Reference the configured dbConnection object -->
    </bean>
</beans>
After the DI Container starts, it will first read this configuration file and immediately know: Oh, I need to create two objects, dbConnection and userService, and userService can only be created after dbConnection is created. This is the role of configuration parsing — to let the container know what to do.

2. Create Objects: Dynamic Creation Without Hard Coding
Some people may ask: If I have dozens or hundreds of objects to create, do I have to write a factory class for each object? That would be too troublesome. The number of classes will explode, and maintenance will be a headache.

In fact, there’s no need to be so troublesome. We only need to write a unified factory class, called BeansFactory for example, to be responsible for creating all objects. And don’t worry that this factory class will become more and more bloated — because we will use Java’s reflection mechanism.

To put it simply, reflection allows us to dynamically load a class and create an object according to the full class name (such as com.xzg.UserService) when the program is running, without writing new UserService() hard-coded in the code. Whether you want to create 1 object or 100 objects, the code of BeansFactory doesn’t need to be changed, as long as it’s clearly written in the configuration file. This is also the key to the versatility of the DI Container.

3. Manage Objects: Control the “Lifetime” of Objects
Creating objects is only the first step. The DI Container also has to manage the “lifetime” of these objects — that is, the life cycle. As we said in the previous lesson, a simple factory can return a new object or a singleton object (the same one is returned every time), and the DI Container also supports this function, and it’s more flexible.

For example, in Spring, we can set it through the scope attribute:

scope="singleton": Singleton mode, the object is created only once. Every subsequent request will return the same object (default configuration). For example, a database connection pool only needs to be created once, no need to create a new one every time.
scope="prototype": Prototype mode, a new object is created and returned every time someone requests the object. For example, the request object in the Service needs a new one for each request to avoid thread safety issues.
In addition, there is a commonly used function — lazy loading (lazy-init):

lazy-init="true": Lazy loading, the object is created only when it is used. For example, if an object is rarely used, there's no need to create it when the program starts, which saves memory.
lazy-init="false": Eager loading, the object is created as soon as the program starts. It can be directly obtained when used later, saving creation time (default configuration).
There’s also a more practical one: you can configure initialization methods and destruction methods for objects. For example, after creating an object, automatically call the init() method to load the configuration; when the object is to be destroyed, automatically call the destroy() method to release resources (such as closing the database connection). There’s no need for us to call them manually, which is very convenient.

Hands-On Implementation: A Simple DI Container (Core Code Can Be Run Directly)
After talking about the core functions, the most critical part comes next — implementing a simple DI Container by hand. We don’t need to make it as complex as Spring, just implement the most core functions: read xml configuration, create objects with reflection, and manage singletons and lazy loading.

The entire implementation process is divided into 4 steps, each step is very simple. You can run it by following along. Let’s do it step by step.

Press enter or click to view image in full size

1. First Define a Minimum Prototype (Clarify the Functions to Be Implemented)
Our goal is not to write a perfect DI Container, but to understand its principle. So we only implement the most core functions:

Can read xml configuration files and parse out the objects to be created and their dependency relationships;
Can create objects according to the configuration through reflection and handle dependencies between objects;
Can manage singletons and lazy loading, supporting scope and lazy-init configuration;
Provide a simple external interface to obtain objects through the bean’s id.
Moreover, our usage method should be as similar as possible to Spring, so that when everyone uses Spring in the future, they can quickly associate it with the underlying principles. For example, use it like this:

public class Demo {
    public static void main(String[] args) {
        // 1. Load the configuration file and initialize the DI Container
        ApplicationContext context = new ClassPathXmlApplicationContext("beans.xml");
        // 2. Get the userService object from the container (no need to new it yourself, nor manage dependencies)
        UserService userService = (UserService) context.getBean("userService");
        // 3. Use the object directly
        userService.queryUser();
    }
}
Run this code, and if it can normally output the database address, it means our DI Container implementation is successful.

2. Provide an Execution Entry (ApplicationContext Interface and Implementation Class)
For the external entry, we use two classes to implement it: one is the ApplicationContext interface, which defines the core methods; the other is the ClassPathXmlApplicationContext implementation class, which is responsible for loading the configuration and initializing the container.

The advantage of this design is that if we want to support other configuration formats (such as properties) in the future, we only need to write another implementation class without modifying the original code, which conforms to the Open/Closed Principle.

// External interface, only exposing a getBean method, simple and intuitive
public interface ApplicationContext {
    // Get the object from the container according to the bean's id
    Object getBean(String beanId);
}


// Implementation class: Load xml configuration from the classpath and initialize the container
public class ClassPathXmlApplicationContext implements ApplicationContext {
    // Core factory, responsible for creating and managing objects
    private BeansFactory beansFactory;
    // Configuration parser, responsible for parsing xml configuration
    private BeanConfigParser configParser;
    // Constructor: Pass in the configuration file path (such as "beans.xml")
    public ClassPathXmlApplicationContext(String configLocation) {
        this.beansFactory = new BeansFactory();
        this.configParser = new XmlBeanConfigParser();
        // Load and parse the configuration, initialize objects
        loadAndParseConfig(configLocation);
    }
    // Load the configuration file, parse it, and hand it over to BeansFactory to create objects
    private void loadAndParseConfig(String configLocation) {
        InputStream in = null;
        try {
            // Read the configuration file from the classpath (such as beans.xml in the resources directory)
            in = this.getClass().getResourceAsStream("/" + configLocation);
            if (in == null) {
                throw new RuntimeException("Configuration file not found: " + configLocation);
            }
            // Parse the xml configuration and convert it into a unified BeanDefinition format (to be discussed later)
            List<BeanDefinition> beanDefinitions = configParser.parse(in);
            // Hand over the parsing results to BeansFactory to create objects
            beansFactory.addBeanDefinitions(beanDefinitions);
        } finally {
            // Close the input stream to avoid resource leakage
            if (in != null) {
                try {
                    in.close();
                } catch (IOException e) {
                    System.err.println("Failed to close the configuration file input stream: " + e.getMessage());
                }
            }
        }
    }
    // Implement the getBean method, directly delegate to BeansFactory
    @Override
    public Object getBean(String beanId) {
        return beansFactory.getBean(beanId);
    }
}
The role of this step is to connect the entire process: load the configuration file → parse the configuration → hand it over to the factory to create objects. Only the getBean method is exposed externally, and users don’t need to care about internal details.

3. Configuration Parsing: Convert Xml to a Format Understandable by the Container
The xml configuration file is for us humans to read, but the container can’t understand it. So we need a parser to convert the tags in xml (such as <bean>, <constructor-arg>) into Java objects understandable by the container — we name it BeanDefinition, which is used to store all information of a single bean (id, class path, constructor parameters, scope, lazy-init, etc.).

Here we only provide the core code framework. The specific details of xml parsing (such as using DOM or SAX) can be supplemented by yourself. The key is to understand the process of “parsing configuration → standardized storage”.

// Configuration parser interface, defining parsing methods
public interface BeanConfigParser {
    // Parse configuration from input stream (read xml file)
    List<BeanDefinition> parse(InputStream in);
    // Parse configuration from string (for testing, optional)
    List<BeanDefinition> parse(String configContent);
}


// Xml configuration parser implementation class
public class XmlBeanConfigParser implements BeanConfigParser {
    @Override
    public List<BeanDefinition> parse(InputStream in) {
        // The specific code for xml parsing is omitted here (such as using DOM parsing)
        // Core logic: Read the <bean> tags, extract attributes such as id, class, constructor-arg
        // Convert to BeanDefinition objects and add to the list
        String configContent = streamToString(in); // Convert input stream to string, implementation omitted
        return parse(configContent);
    }
    @Override
    public List<BeanDefinition> parse(String configContent) {
        List<BeanDefinition> beanDefinitions = new ArrayList<>();
        // TODO: Supplement xml parsing logic, only the example idea is given here
        // 1. Parse the xml string and get all <bean> nodes
        // 2. For each <bean> node, extract attributes such as id, class, scope, lazy-init
        // 3. Parse the <constructor-arg> nodes and get the constructor parameters
        // 4. Create BeanDefinition objects and add them to the list
        return beanDefinitions;
    }
    // Convert input stream to string (implementation omitted, can be supplemented by yourself)
    private String streamToString(InputStream in) {
        //... Implementation code omitted
        return "";
    }
}
// Core data structure: Store all configuration information of a single bean (format understandable by the container)
public class BeanDefinition {
    private String id; // Unique identifier of the bean (such as "userService")
    private String className; // Full class name (such as "com.xzg.UserService")
    private List<ConstructorArg> constructorArgs = new ArrayList<>(); // Constructor parameters
    private Scope scope = Scope.SINGLETON; // Default singleton
    private boolean lazyInit = false; // Default eager loading
    // Judge whether it is a singleton
    public boolean isSingleton() {
        return Scope.SINGLETON.equals(this.scope);
    }
    // Scope enumeration: Singleton/Prototype
    public enum Scope {
        SINGLETON, PROTOTYPE
    }
    // Inner class: Store constructor parameter information
    public static class ConstructorArg {
        private boolean isRef; // Whether it references another bean (such as userService references dbConnection)
        private Class<?> type; // Parameter type (such as String, int)
        private Object value; // Parameter value (if it is a reference, it is the bean's id)
        // Getter/setter methods, implementation omitted
        public boolean isRef() { return isRef; }
        public void setRef(boolean ref) { isRef = ref; }
        public Class<?> getType() { return type; }
        public void setType(Class<?> type) { this.type = type; }
        public Object getValue() { return value; }
        public void setValue(Object value) { this.value = value; }
    }
    // Getter/setter methods, implementation omitted
    public String getId() { return id; }
    public void setId(String id) { this.id = id; }
    public String getClassName() { return className; }
    public void setClassName(String className) { this.className = className; }
    public List<ConstructorArg> getConstructorArgs() { return constructorArgs; }
    public void setConstructorArgs(List<ConstructorArg> constructorArgs) { this.constructorArgs = constructorArgs; }
    public Scope getScope() { return scope; }
    public void setScope(Scope scope) { this.scope = scope; }
    public boolean isLazyInit() { return lazyInit; }
    public void setLazyInit(boolean lazyInit) { this.lazyInit = lazyInit; }
}
The key here is the BeanDefinition class — it is equivalent to the “bridge” between the xml configuration and the factory class. The parser converts xml into BeanDefinition, and the factory class creates objects according to BeanDefinition. The division of labor is clear and the logic is clear.

4. Core Factory Class: BeansFactory (Core of Object Creation and Management)
This is the “heart” of our entire DI Container. The creation of all objects, dependency processing, and life cycle management are all implemented here. The core logic is very simple:

1. Receive the parsed list of BeanDefinition;

2. For non-lazy loaded singleton objects, create them when the program starts and cache them;

3. For lazy loaded or prototype objects, create them when getBean is called;

4. Handle object dependencies: If A depends on B, create B first, then create A.

The specific code is as follows, with detailed comments for everyone to read slowly:

import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;


public class BeansFactory {
    // Singleton object cache: key=beanId, value=created singleton object (thread-safe)
    private final ConcurrentHashMap<String, Object> singletonCache = new ConcurrentHashMap<>();
    // Bean configuration cache: key=beanId, value=BeanDefinition (stores configuration information)
    private final ConcurrentHashMap<String, BeanDefinition> beanDefCache = new ConcurrentHashMap<>();
    // Add the parsed list of BeanDefinition and initialize non-lazy loaded singletons
    public void addBeanDefinitions(List<BeanDefinition> beanDefList) {
        if (beanDefList == null || beanDefList.isEmpty()) {
            return;
        }
        // 1. First cache all BeanDefinitions
        for (BeanDefinition beanDef : beanDefList) {
            beanDefCache.putIfAbsent(beanDef.getId(), beanDef);
        }
        // 2. Initialize non-lazy loaded singleton objects (created when the program starts)
        for (BeanDefinition beanDef : beanDefList) {
            if (!beanDef.isLazyInit() && beanDef.isSingleton()) {
                createBean(beanDef); // Will be automatically put into the singleton cache after creation
            }
        }
    }
    // Get the object according to the beanId: Core entry
    public Object getBean(String beanId) {
        // 1. First get the BeanDefinition from the configuration cache
        BeanDefinition beanDef = beanDefCache.get(beanId);
        if (beanDef == null) {
            throw new NoSuchBeanException("No Bean named [" + beanId + "] found, please check the configuration file");
        }
        // 2. Create and return the object (handle singleton, prototype, lazy loading)
        return createBean(beanDef);
    }
    // Core method: Create objects according to BeanDefinition, handle dependencies and life cycle
    protected Object createBean(BeanDefinition beanDef) {
        String beanId = beanDef.getId();
        // 1. If it is a singleton and already cached, return the cached object directly
        if (beanDef.isSingleton() && singletonCache.containsKey(beanId)) {
            return singletonCache.get(beanId);
        }
        Object bean = null;
        try {
            // 2. Dynamically load the class through reflection (according to the full class name)
            Class<?> beanClass = Class.forName(beanDef.getClassName());
            // 3. Handle constructor parameters (if there are dependencies, create the dependent objects first)
            List<BeanDefinition.ConstructorArg> constructorArgs = beanDef.getConstructorArgs();
            // 3.1 Prepare the parameter types and parameter values of the constructor
            Class<?>[] argTypes = new Class<?>[constructorArgs.size()];
            Object[] argValues = new Object[constructorArgs.size()];
            for (int i = 0; i < constructorArgs.size(); i++) {
                BeanDefinition.ConstructorArg arg = constructorArgs.get(i);
                if (arg.isRef()) {
                    // 3.2 If it references another Bean, recursively create the referenced Bean
                    String refBeanId = (String) arg.getValue();
                    argValues[i] = getBean(refBeanId); // Recursively call getBean
                    argTypes[i] = argValues[i].getClass();
                } else {
                    // 3.3 If it is a normal parameter (such as String, int), use the configured value directly
                    argTypes[i] = arg.getType();
                    argValues[i] = arg.getValue();
                }
            }
            // 4. Create the object through the constructor (core operation of reflection)
            if (constructorArgs.isEmpty()) {
                // No-argument constructor: Create the object directly
                bean = beanClass.newInstance();
            } else {
                // Parameterized constructor: Pass in parameter types and parameter values to create the object
                Constructor<?> constructor = beanClass.getConstructor(argTypes);
                bean = constructor.newInstance(argValues);
            }
            // 5. If it is a singleton, put it into the cache after creation
            if (beanDef.isSingleton()) {
                singletonCache.putIfAbsent(beanId, bean);
                return singletonCache.get(beanId); // Get from the cache to ensure singleton
            }
        } catch (ClassNotFoundException e) {
            throw new BeanCreateException("Failed to create Bean [" + beanId + "]: The corresponding class was not found", e);
        } catch (InstantiationException | IllegalAccessException e) {
            throw new BeanCreateException("Failed to create Bean [" + beanId + "]: Class initialization exception", e);
        } catch (NoSuchMethodException e) {
            throw new BeanCreateException("Failed to create Bean [" + beanId + "]: The corresponding constructor was not found", e);
        } catch (InvocationTargetException e) {
            throw new BeanCreateException("Failed to create Bean [" + beanId + "]: Constructor call exception", e);
        }
        // 6. Prototype object: Return the newly created object directly (no caching)
        return bean;
    }
    // Custom exception: Bean not found (more business-friendly than using RuntimeException)
    public static class NoSuchBeanException extends RuntimeException {
        public NoSuchBeanException(String message) {
            super(message);
        }
    }
    // Custom exception: Bean creation failed
    public static class BeanCreateException extends RuntimeException {
        public BeanCreateException(String message, Throwable cause) {
            super(message, cause);
        }
    }
}
At this point, our simple DI Container implementation is complete. Integrate this code with the specific logic of xml parsing, and it can run normally — run the previous Demo class, and you can successfully obtain the UserService object and call the queryUser method to output the database address.

Final Summary: Remember the Core Points, and Don’t Panic in Interview
Today, we extended from the Factory Pattern, talked about the principle of the DI Container, and also implemented a simple DI Container by hand. In fact, the DI Container is not as mysterious as everyone thinks. Its core is “super factory + reflection + configuration parsing + life cycle management”.

Finally, let’s emphasize a few core points. Remember these, and you can calmly respond whether you are understanding Spring’s IOC Container or being asked in an interview:

1. The relationship between DI Container and Factory Pattern: The DI Container is an “upgraded version” of the Factory Pattern. It is based on the Factory Pattern at the bottom, but it is more powerful than ordinary factories. It can manage all objects of the application, and also handle configuration and life cycle;

2. Core functions of the DI Container: Parse configuration (know what to create), create objects (dynamically create with reflection), manage life cycle (singleton/prototype, lazy loading, initialization/destruction);

Implementation key points: Reflection mechanism (dynamically create objects without hard coding), BeanDefinition (standardized configuration information), singleton cache (manage singleton objects);

4. Core value: Decoupling. Completely separate the creation, assembly, and management of objects from the business code. Programmers no longer have to worry about tedious object creation and dependency processing, and can concentrate on writing business logic.

In fact, the underlying principles of many frameworks are very simple. As long as we are willing to disassemble and implement them by hand, we can see through their essence. In the next lesson, we will talk about another extended scenario of the Factory Pattern, so stay tuned!