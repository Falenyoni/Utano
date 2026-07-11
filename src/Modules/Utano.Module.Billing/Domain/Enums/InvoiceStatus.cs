namespace Utano.Module.Billing.Domain.Enums;

public enum InvoiceStatus { Draft, Issued, PartiallyPaid, Paid, Overdue, Void }
public enum LineItemType { Consultation, Procedure, Medication, LabTest, Consumable, Other }
public enum PaymentMethod { Cash, EcoCash, ZIPIT, ZimSwitch, Card, MedicalAid, BankTransfer, Other }
public enum PaymentPlanFrequency { Weekly, Biweekly, Monthly }
public enum PaymentPlanStatus { Active, Completed, Defaulted, Cancelled }
public enum MedAidClaimStatus { None, Pending, Approved, Rejected }
