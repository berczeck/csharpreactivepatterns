using System;
using System.Collections.Generic;
using Akka.Actor;

namespace Routing_Slip
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("system"))
            {
                var contactKeeper =
                    system.ActorOf<ContactKeeper>("contactKeeper");
                var creditChecker =
                    system.ActorOf<CreditChecker>("creditChecker");
                var customerVault =
                    system.ActorOf<CustomerVault>("customerVault");
                var servicePlanner =
                    system.ActorOf<ServicePlanner>("servicePlanner");

                var processId = Guid.NewGuid();
                var step1 = new ProcessStep("create_customer", customerVault);
                var step2 = new ProcessStep("set_up_contact_info", contactKeeper);
                var step3 = new ProcessStep("select_service_plan", servicePlanner);
                var step4 = new ProcessStep("check_credit", creditChecker);

                var registrationProcess =
                    RegistrationProcess.Create(processId.ToString(), new List<ProcessStep> { step1, step2, step3, step4 });

                var registrationData = new RegistrationData(
                    new CustomerInformation("ABC, Inc.", "123-45-6789"),
                    new ContactInformation(new PostalAddress("123 Main Street", "Suite 100", "Boulder", "CO", "80301"), 
                    new Telephone("303-555-1212")),
                    new ServiceOption("99-1203", "A description of 99-1203."));

                var registerCustomer = new RegisterCustomer(registrationData, registrationProcess);

                registrationProcess.NextStep().Processor.Tell(registerCustomer);

                Console.ReadLine();

            }
        }
    }

    public class ServicePlanner : ReceiveActor
    {
        public ServicePlanner()
        {
            Receive<RegisterCustomer>(x =>
            {
                var serviceOption = x.RegistrationData.ServiceOption;
                Console.WriteLine($"ServicePlanner: handling register customer to plan a new customer service: {serviceOption}");
                x.Advance();
            });
        }
    }
    public class CustomerVault : ReceiveActor
    {
        public CustomerVault()
        {
            Receive<RegisterCustomer>(x =>
            {
                var customerInformation = x.RegistrationData.CustomerInformation;
                Console.WriteLine($"CustomerVault: handling register customer to create a new custoner: {customerInformation}");
                x.Advance();
            });
        }
    }
    public class ContactKeeper : ReceiveActor
    {
        public ContactKeeper()
        {
            Receive<RegisterCustomer>(x =>
            {
                var contactInfo = x.RegistrationData.ContactInformation;
                Console.WriteLine($"ContactKeeper: handling register customer to keep contact information: {contactInfo}");
                x.Advance();
            });
        }
    }
    public class CreditChecker : ReceiveActor
    {
        public CreditChecker()
        {
            Receive<RegisterCustomer>(x =>
            {
                var federalTaxId = x.RegistrationData.CustomerInformation.FederalTaxId;
                Console.WriteLine($"CreditChecker: handling register customer to perform credit check: {federalTaxId}");
                x.Advance();
            });
        }
    }
    public class RegisterCustomer
    {
        public RegistrationData RegistrationData { get; }
        public RegistrationProcess RegistrationProcess { get; }
        public RegisterCustomer(RegistrationData registrationData, RegistrationProcess registrationProcess)
        {
            RegistrationData = registrationData;
            RegistrationProcess = registrationProcess;
        }
        public void Advance()
        {
            var advancedProcess = RegistrationProcess.StepCompleted();
            if(!advancedProcess.IsCompleted)
            {
                advancedProcess.NextStep().Processor.Tell(new RegisterCustomer(RegistrationData, advancedProcess));
            }
        }
    }
    public class RegistrationProcess
    {
        public string ProcessId { get; }
        public List<ProcessStep> ProcessSteps { get; }
        public int CurrentStep { get; private set; }

        public bool IsCompleted => CurrentStep >= ProcessSteps.Count;
        public ProcessStep NextStep()
        {
            if (IsCompleted)
            {
                throw new Exception("Process had already completed.");
            }
            else
            {
                return ProcessSteps[CurrentStep];
            }
        }
        public RegistrationProcess StepCompleted() 
            => new RegistrationProcess(ProcessId, ProcessSteps, ++CurrentStep);
        private RegistrationProcess(string processId, List<ProcessStep> processSteps, int currentStep)
        {
            ProcessId = processId;
            ProcessSteps = processSteps;
            CurrentStep = currentStep;
        }

        public static RegistrationProcess Create(string processId, List<ProcessStep> processSteps) 
            => new RegistrationProcess(processId, processSteps, 0);
    }
    public class ProcessStep
    {
        public string Name { get; }
        public IActorRef Processor { get; }
        public ProcessStep(string name, IActorRef processor)
        {
            Name = name;
            Processor = processor;
        }
    }
    public class RegistrationData
    {
        public CustomerInformation CustomerInformation { get; }
        public ContactInformation ContactInformation { get; }
        public ServiceOption ServiceOption { get;  }
        public RegistrationData(CustomerInformation customerInformation, ContactInformation contactInformation, ServiceOption serviceOption)
        {
            CustomerInformation = customerInformation;
            ContactInformation = contactInformation;
            ServiceOption = serviceOption;
        }
    }
    public class ServiceOption
    {
        public string Id { get; }
        public string Description { get; }
        public ServiceOption(string id, string description)
        {
            Id = id;
            Description = description;
        }
    }
    public class ContactInformation
    {
        public PostalAddress PostalAddress { get;  }
        public Telephone Telephone { get; }
        public ContactInformation(PostalAddress postalAddress, Telephone telephone)
        {
            PostalAddress = postalAddress;
            Telephone = telephone;
        }
    }
    public class CustomerInformation
    {
        public string Name { get; }
        public string FederalTaxId { get; }
        public CustomerInformation(string name, string federalTaxId)
        {
            Name = name;
            FederalTaxId = federalTaxId;
        }
    }
    public class Telephone
    {
        public string Number { get; }
        public Telephone(string number)
        {
            Number = number;
        }
    }
    public class PostalAddress
    {
        public string Address1 { get; }
        public string Address2 { get; }
        public string City { get; }
        public string State { get; }
        public string ZipCode { get; }

        public PostalAddress(string address1, string address2, string city, string state, string zipCode)
        {
            Address1 = address1;
            Address2 = address2;
            City = city;
            State = state;
            ZipCode = zipCode;
        }
    }
}
