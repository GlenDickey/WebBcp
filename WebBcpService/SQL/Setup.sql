use WebBcp;
go

if exists(select 1 from sysobjects where name = 'BcpColumnFormat')
begin
	drop table BcpColumnFormat;
end;

if exists(select 1 from sysobjects where name = 'BcpTableFormat')
begin
	drop table BcpTableFormat;
end;
go

create table BcpTableFormat
(
	Id int identity(1,1) primary key,
	Name nvarchar(100) not null,
)
go

create table BcpColumnFormat
(
	Id int identity(1,1) primary key,
	BcpTableFormatId int not null foreign key references BcpTableFormat(Id),
	FromColumnName nvarchar(250) not null,
	ToColumnName nvarchar(250) not null,
	ToDataType nvarchar(100) not null,
	Sequence int not null
)
go

-- example formats
insert into BcpTableFormat (name) values ('ORDER FORMAT');
insert into BcpTableFormat (name) values ('CLIENT FORMAT');

-- orders ----------------------------------------------------
begin 
	declare @bcpTableFormatId int;

	select @bcpTableFormatId = t.Id
	from BcpTableFormat t
	where t.Name = 'ORDER FORMAT';

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '1', 'OrderId', 'int', 1);

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '2', 'Name', 'varchar(200)', 2);

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '3', 'DeliveryCity', 'varchar(200)', 3);

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '4', 'DeliveryState', 'varchar(200)', 4);

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '5', '', '', 5);

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '6', 'Price', 'money', 6);
end;
go

-- client ----------------------------------------------------
begin
	declare @bcpTableFormatId int;

	select @bcpTableFormatId = t.Id
	from BcpTableFormat t
	where t.Name = 'CLIENT FORMAT';

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '1', 'ClientId', 'int', 1);

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '2', 'FirstName', 'varchar(200)', 2);

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '3', 'LastName', 'varchar(200)', 3);

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '4', 'City', 'varchar(200)', 4);

	insert into BcpColumnFormat ( BcpTableFormatId, FromColumnName, ToColumnName, ToDataType, Sequence)
	values ( @bcpTableFormatId, '5', 'Price', 'money', 5);
end;
go