-- ================================================================
-- RaceDay Database Script
-- PROG6212 - PoE Part 1
-- Matches RaceDay ERD: Users, Venues, Events, Categories, Enrolments, Results

-- ================================================================

-- Drop existing objects if re-running this script on a non-clean DB (safe to skip on first run)
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Venues', 'U') IS NOT NULL DROP TABLE dbo.Venues;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- ================================================================
-- TABLE: Users
-- Stores both Organisers and Participants, distinguished by Role.
-- ================================================================
CREATE TABLE dbo.Users (
    UserID          INT IDENTITY(1,1) PRIMARY KEY,
    FirstName       VARCHAR(50)     NOT NULL,
    LastName        VARCHAR(50)     NOT NULL,
    Email           VARCHAR(100)    NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255)    NOT NULL,
    Role            VARCHAR(20)     NOT NULL CHECK (Role IN ('Organiser', 'Participant')),
    PhoneNumber     VARCHAR(20)     NULL,
    DateRegistered  DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

-- ================================================================
-- TABLE: Venues
-- Normalised location data, reusable across multiple events.
-- ================================================================
CREATE TABLE dbo.Venues (
    VenueID         INT IDENTITY(1,1) PRIMARY KEY,
    VenueName       VARCHAR(100)    NOT NULL,
    AddressLine     VARCHAR(150)    NOT NULL,
    City            VARCHAR(50)     NOT NULL,
    Province        VARCHAR(50)     NOT NULL
);
GO

-- ================================================================
-- TABLE: Events
-- Created and managed by an Organiser, hosted at a Venue.
-- ================================================================
CREATE TABLE dbo.Events (
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    EventName       VARCHAR(100)    NOT NULL,
    Description     VARCHAR(500)    NULL,
    EventDate       DATE            NOT NULL,
    EventType       VARCHAR(20)     NOT NULL CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    Distance        DECIMAL(5,2)    NOT NULL,
    OrganiserID     INT             NOT NULL,
    VenueID         INT             NOT NULL,
    BannerImageURL  VARCHAR(255)    NULL,
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Events_Venue     FOREIGN KEY (VenueID)     REFERENCES dbo.Venues(VenueID)
);
GO

-- ================================================================
-- TABLE: Categories
-- Age or distance categories defined per Event by its Organiser.
-- ================================================================
CREATE TABLE dbo.Categories (
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT             NOT NULL,
    CategoryName    VARCHAR(50)     NOT NULL,
    Description     VARCHAR(200)    NULL,
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventID) REFERENCES dbo.Events(EventID)
);
GO

-- ================================================================
-- TABLE: Enrolments
-- Associative entity resolving the many-to-many between
-- Participants (Users) and Events, via a selected Category.
-- ================================================================
CREATE TABLE dbo.Enrolments (
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    EventID         INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    EnrolmentStatus VARCHAR(20)     NOT NULL DEFAULT 'Pending' CHECK (EnrolmentStatus IN ('Pending', 'Confirmed')),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantID) REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Event       FOREIGN KEY (EventID)       REFERENCES dbo.Events(EventID),
    CONSTRAINT FK_Enrolments_Category    FOREIGN KEY (CategoryID)    REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Enrolments_ParticipantEvent UNIQUE (ParticipantID, EventID)
);
GO

-- ================================================================
-- TABLE: Results
-- One-to-one with Enrolments. Captured by an Organiser after the event.
-- ================================================================
CREATE TABLE dbo.Results (
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NOT NULL,
    FinishPosition  INT             NOT NULL,
    TotalFinishers  INT             NULL,
    CapturedBy      INT             NOT NULL,
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolments(EnrolmentID),
    CONSTRAINT FK_Results_CapturedBy FOREIGN KEY (CapturedBy) REFERENCES dbo.Users(UserID)
);
GO

-- ================================================================
-- SEED DATA
-- ================================================================

-- Users: 2 Organisers, 2 Participants
INSERT INTO dbo.Users (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber) VALUES
('Sipho', 'Ndlovu', 'sipho.ndlovu@raceday.co.za', 'HASHED_PASSWORD_1', 'Organiser', '0821234567'),
('Amanda', 'Botha', 'amanda.botha@raceday.co.za', 'HASHED_PASSWORD_2', 'Organiser', '0837654321'),
('Thabo', 'Mokoena', 'thabo.mokoena@gmail.com', 'HASHED_PASSWORD_3', 'Participant', '0721112222'),
('Lindiwe', 'Dube', 'lindiwe.dube@gmail.com', 'HASHED_PASSWORD_4', 'Participant', '0733334444');
GO

-- Venues
INSERT INTO dbo.Venues (VenueName, AddressLine, City, Province) VALUES
('Kirstenbosch Gardens', 'Rhodes Drive, Newlands', 'Cape Town', 'Western Cape'),
('Union Buildings Grounds', 'Government Ave', 'Pretoria', 'Gauteng'),
('Comrades Marathon Route Start', '2 Old Fort Road', 'Pietermaritzburg', 'KwaZulu-Natal');
GO

-- Events: 3 Events (2 by Organiser 1, 1 by Organiser 2)
INSERT INTO dbo.Events (EventName, Description, EventDate, EventType, Distance, OrganiserID, VenueID) VALUES
('Cape Town Spring Fun Run', 'A scenic community fun run through Kirstenbosch.', '2026-10-18', 'Run', 10.00, 1, 1),
('Pretoria Charity Walk', 'A family-friendly charity walk supporting local schools.', '2026-11-02', 'Walk', 5.00, 1, 2),
('Comrades Prep Cycle Challenge', 'A cycling event along part of the Comrades route.', '2026-11-22', 'Cycle', 21.10, 2, 3);
GO

-- Categories: at least one per event
INSERT INTO dbo.Categories (EventID, CategoryName, Description) VALUES
(1, '10km Run', 'Standard 10km distance category.'),
(1, 'Under 20', 'Junior category for participants under 20.'),
(2, '5km Walk', 'Standard 5km walking category.'),
(2, 'Senior', 'Category for participants aged 60 and above.'),
(3, '21km Cycle', 'Half-distance cycling category.');
GO

-- Enrolments: sample Participants enrolling in Events
INSERT INTO dbo.Enrolments (ParticipantID, EventID, CategoryID, EnrolmentStatus) VALUES
(3, 1, 1, 'Confirmed'),
(4, 1, 2, 'Confirmed'),
(3, 2, 3, 'Pending'),
(4, 3, 5, 'Confirmed');
GO

-- Results: sample results captured for completed enrolments
INSERT INTO dbo.Results (EnrolmentID, FinishTime, FinishPosition, TotalFinishers, CapturedBy) VALUES
(1, '00:52:14', 47, 312, 1),
(4, '01:10:05', 12, 88, 2);
GO
