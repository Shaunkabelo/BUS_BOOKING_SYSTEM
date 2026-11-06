-- Run this entire script in SSMS against SQL Server / SQL Express.
-- Creates a dedicated database. Safe to re-run; existing records are retained.
USE master;
GO
IF DB_ID(N'BusBooking') IS NULL CREATE DATABASE BusBooking;
GO
USE BusBooking;
GO
IF SCHEMA_ID(N'travel') IS NULL EXEC(N'CREATE SCHEMA travel');
GO
IF OBJECT_ID(N'travel.Users') IS NULL
CREATE TABLE travel.Users (
 Id int IDENTITY PRIMARY KEY, Name nvarchar(100) NOT NULL,
 Email nvarchar(254) NOT NULL UNIQUE, Phone nvarchar(30) NOT NULL,
 PasswordHash nvarchar(500) NOT NULL,
 Role varchar(20) NOT NULL DEFAULT 'Customer' CHECK (Role IN ('Customer','Admin'))
);
IF OBJECT_ID(N'travel.Buses') IS NULL
CREATE TABLE travel.Buses (
 Id int IDENTITY PRIMARY KEY, BusNumber nvarchar(30) NOT NULL UNIQUE,
 Company nvarchar(100) NOT NULL, BusType nvarchar(50) NOT NULL,
 TotalSeats int NOT NULL CHECK (TotalSeats BETWEEN 10 AND 80)
);
IF OBJECT_ID(N'travel.Trips') IS NULL
CREATE TABLE travel.Trips (
 Id int IDENTITY PRIMARY KEY, BusId int NOT NULL REFERENCES travel.Buses(Id),
 Origin nvarchar(100) NOT NULL, Destination nvarchar(100) NOT NULL,
 BoardingPoint nvarchar(200) NOT NULL, ArrivalPoint nvarchar(200) NOT NULL,
 Departure datetime2 NOT NULL, Arrival datetime2 NOT NULL,
 Fare decimal(10,2) NOT NULL CHECK (Fare > 0),
 CHECK (Origin <> Destination), CHECK (Arrival > Departure)
);
IF OBJECT_ID(N'travel.Reservations') IS NULL
CREATE TABLE travel.Reservations (
 Id int IDENTITY PRIMARY KEY, Reference uniqueidentifier NOT NULL UNIQUE,
 RequestId uniqueidentifier NOT NULL UNIQUE,
 UserId int NOT NULL REFERENCES travel.Users(Id), TripId int NOT NULL REFERENCES travel.Trips(Id),
 Total decimal(10,2) NOT NULL CHECK (Total > 0),
 Status varchar(20) NOT NULL CHECK (Status IN ('DemoConfirmed','Cancelled')),
 CreatedUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID(N'travel.Seats') IS NULL
CREATE TABLE travel.Seats (
 Id int IDENTITY PRIMARY KEY, ReservationId int NOT NULL REFERENCES travel.Reservations(Id),
 TripId int NOT NULL REFERENCES travel.Trips(Id), SeatNumber int NOT NULL CHECK (SeatNumber BETWEEN 1 AND 80),
 PassengerName nvarchar(100) NOT NULL, IsActive bit NOT NULL DEFAULT 1
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ActiveTripSeat' AND object_id = OBJECT_ID('travel.Seats'))
CREATE UNIQUE INDEX UX_ActiveTripSeat ON travel.Seats(TripId, SeatNumber) WHERE IsActive = 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TripSearch' AND object_id = OBJECT_ID('travel.Trips'))
CREATE INDEX IX_TripSearch ON travel.Trips(Origin, Destination, Departure);
GO
-- Fictional operators and fares for a personal/demo project.
IF NOT EXISTS (SELECT 1 FROM travel.Buses)
INSERT travel.Buses (BusNumber,Company,BusType,TotalSeats) VALUES
 ('DEMO-GP-01','Mzansi Express (Demo)','Luxury',40),
 ('DEMO-GP-02','Coastal Cruiser (Demo)','Sleeper',36),
 ('DEMO-GP-03','City Connect (Demo)','Economy',48),
 ('DEMO-GP-04','Mzansi Express (Demo)','Luxury',40);
GO
-- Add the next 30 days of sample services without duplicating existing ones.
DECLARE @day int = 1, @date date = CONVERT(date,DATEADD(hour,2,SYSUTCDATETIME()));
WHILE @day <= 30
BEGIN
 INSERT travel.Trips (BusId,Origin,Destination,BoardingPoint,ArrivalPoint,Departure,Arrival,Fare)
 SELECT b.Id,r.Origin,r.Destination,r.Boarding,r.ArrivalPoint,
 DATEADD(hour,r.Hour,CAST(DATEADD(day,@day,@date) AS datetime2)),
 DATEADD(hour,r.Hour+r.Duration,CAST(DATEADD(day,@day,@date) AS datetime2)),r.Fare
 FROM (VALUES
 ('DEMO-GP-01','Johannesburg','Durban','Park Station','Durban Station',8,8,350),
 ('DEMO-GP-02','Johannesburg','Durban','Park Station','Durban Station',19,9,480),
 ('DEMO-GP-03','Durban','Johannesburg','Durban Station','Park Station',9,8,320),
 ('DEMO-GP-04','Johannesburg','Cape Town','Park Station','Cape Town Station',12,19,780)
 ) r(BusNumber,Origin,Destination,Boarding,ArrivalPoint,Hour,Duration,Fare)
 JOIN travel.Buses b ON b.BusNumber=r.BusNumber
 WHERE NOT EXISTS (SELECT 1 FROM travel.Trips t WHERE t.BusId=b.Id
 AND t.Departure=DATEADD(hour,r.Hour,CAST(DATEADD(day,@day,@date) AS datetime2)));
 SET @day += 1;
END;
GO
-- Register through the website first. Then promote YOUR account (replace the email):
-- UPDATE travel.Users SET Role='Admin' WHERE Email='you@example.com';
-- Sign out and sign back in after changing a role.
