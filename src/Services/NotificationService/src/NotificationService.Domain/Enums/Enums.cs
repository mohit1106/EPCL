namespace NotificationService.Domain.Enums;

public enum NotificationChannel { Email, SMS, InApp, SignalR }
public enum NotificationStatus { Pending, Sent, Failed }
public enum PriceAlertType { PriceDrop, DailyUpdate }
