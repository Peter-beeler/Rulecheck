# Automated Rule-Checking of fire egress

## Introduction

When desinger design a building model in a BIM design software(i.e. Revit), he should consider whether the design conforms to the building code or legislation, like IBC, Internatinal Building Code. Traditionally, these rules and codes are checked manully by experienced experts and it's time-consuming. In this project, we develop an addin in Revit which can automatically check them or help designer check more easily.

## Building Code Reference

2015 Internatial Buiding Code Chapter10

Link: https://codes.iccsafe.org/content/IBC2015/chapter-10-means-of-egress

Note: Right now only 3 aspects have been implemented, Door dimension check, ceiling and protruding objects and egress travel distance.

##Platform and Environment

OS : Windows 10

BIM Software: Revit 2020

Platform or Packages: Revit API 2020   NETStandard 2.0.3

Programming Language: C#

***IMPORTANT NOTES: The addIn only works on Revit 2020 or above. Please make sure you installed Revit 2020.***

## Installation

### The most easy way 

you can directly download **TranvelAna.dll** file in this repo and put it into a directory you want.

And then create a **TravelAna.addin** file under **C:\ProgramData\Autodesk\Revit\Addins\2020\\**

The content of this file is:

```
<?xml version="1.0" encoding="utf-8"?>

<RevitAddIns>

  <AddIn Type="Command">

       <Name>RuleCheck</Name>

       <FullClassName>RuleCheck.Class1</FullClassName>

       <Text>RuleCheck</Text> 

       <Description>Automated-RuleCheck</Description>

       <VisibilityMode>AlwaysVisible</VisibilityMode>

       <Assembly>[**YOUR DLL FILE PATH**]</Assembly>

       <AddInId>502fe383-2648-4e98-adf8-5e6047f9dc34</AddInId>

    <VendorId>ADSK</VendorId>

    <VendorDescription>Autodesk, Inc, www.autodesk.com</VendorDescription>

  </AddIn>

</RevitAddIns>
```

### Backup Way

Also, if the installation above doesn't work, you can compile the source code youself.

Please check the webpage:

https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/simplecontent/content/lesson-1-the-basic-plug.html

The only thing different is that you need to repalce the codes with codes in Class1.cs in this repo.

## How to do the check and result explaination

### Run

1. Click the External Tools in Addln ribbon:

![TIMΩÿÕº20190901144047](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901144047.png)

2. Then you will see the addIn you create, click it and it will run.

Note: if you want to check the ceiling, please make sure the current model view you open is the Ceiling view. And also select rooms which you want to check the ceiling.

### Explaination

1. First, it will check all doors and theirs' dimensions.

   If there is no problems, it will show:
   
   ![TIMΩÿÕº20190901144633](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901144633.png)

   if not, it will show all ids of doors which have problems:
   
   ![TIMΩÿÕº20190901144728](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901144728.png)

2. Second, it will check all ceiling objects if they have enough headroom.

   If there is no problems, it will show:
   
   ![TIMΩÿÕº20190901144644](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901144644.png)

   If not,  it will show all ids of elements which have problems:
   
   ![TIMΩÿÕº20190901145151](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901145151.png)

3. Finally, it will check travel distance from every to nearest exits

   At first, it will ask you about which rooms can be passe during the egress:

   ![TIMΩÿÕº20190901150927](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901150927.png)

   It will show the travel distance value of each rooms as the following:

   ![TIMΩÿÕº20190901151138](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901151138.png)
   
   If there are no paths from some rooms, it will show:

   ![TIMΩÿÕº20190901151927](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901151927.png)

   Why need to choose rooms? 

   Because if IBC2015, some rooms cannot be used as parts of egress paths.

   For example, if any room can be passed, the result path is like:

   ![TIMΩÿÕº20190901151156](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901151156.png)

   And then if the room with tag "1" cannot be passed, the result egress will be like:

   ![TIMΩÿÕº20190901150227](https://github.com/Peter-beeler/Rulecheck/blob/master/image/TIMΩÿÕº20190901150227.png)

## Code : RuleCheck namepsace

### Description

All the code of this addIn are included in this namespace. It mainly check doors, ceilings and travel distance. It will also show the evacuation simulation in a emergency.

### Modifiable fields

(const double) DOOR_HEIGHT: The required minimum height of a door.

(const double) SINGLE_WIDTH: The reqired width minimum of a door with only one leaf.

(const double) DOUBLE_WIDTH_MIN: The reqired minimum width of a door with 2 leaves.

(const double) DOUBLE_WIDTH_MAX: The reqired maximum width of a door with 2 leaves.

(const double) CEILING_HEIGHT: The minimun headroom of a floor.

### Methods:

(Public Result) Execute(ExternalCommandData commandData, ref string message, ElementSet elements):

The main function which will be the first executed by Revit.



(Public void) void Report(KeyValuePair<List<Room>, List<double>> result, Document doc,IList<ElementId> RoomForbid)

Description : Show a dialog in Revit, containing all room's travel distance to the closest exit.

Input: 

* KeyValuePair<List<Room>, List<double>> result : a key-value data structure, the key is a list which contains all rooms, the value is the travel distance(double) of the rooms.
* Document doc: document of the revit model.
* IList<ElementId> RoomForbid: List of ids of all rooms which people cannot pass through in evacuation.

Return: 

None



(public IEnumerable<Room>) GetRoomsOnLevel(Document doc, ElementId idLevel) 

Description: 

Get all rooms on the current view floor.

Input: 

* ElementId idLevel: The floor's  level Id. 
* Document doc: document of the revit model.

Return:

A Enumerable data-structure containing all rooms on this level.



(public IEnumerable<Element>) GetSpacesOnLevel(Document doc, ElementId idLevel)

 Description: 

Get all spaces on the current view floor.

Input: 

- ElementId idLevel: The floor's  level Id. 
- Document doc: document of the revit model.

Return:

A Enumerable data-structure containing all spaces on this level.



(public Double) calDis(IList<Curve> p) 

Description:

cal the lenght of a travel path

Input:

* IList<Curve> p: a list of curves

Return:

Distance of all the input curves.



(public ElementId) ViewLevel(Document doc)

Description:

get id of the current level

Input:

* Document doc: document of the revit model.

Return:

ElementId of the level in current view.



(public List<ElementId>) GetExits(Document doc)

Description:

get all elementIds which are doors connectiong building to outside.

Input:

* Document doc: document of the revit model.

Return:

 A list of Exits' elementIds.



(public KeyValuePair<List<Room>, List<double>>) Graph(Document doc, IList<ElementId> RoomForbid,KeyValuePair<List<Room>, List<List<XYZ>>> Points)

Description:

The method can calculate when people evacuate, the minimun distances of all points in the floor plan. And also the routes from every room will be shown in Revit. The route will avoid obstacles and some rooms which cannot be passed through.

Input:

* Document doc: document of the revit model.
* IList<ElementId> RoomForbid: ElementIds of rooms which cannot be passed.
* KeyValuePair<List<Room>, List<List<XYZ>>> Points: The rooms and representation points of rooms.

Return:

The rooms and their travel distance. Egress rtoutes are also shown in Revit.



（public IList<ElementId>） GetAllDoors(Document doc, ElementId levelid)

Description:

Use the filter to retrieve all elements belongs to OST_DOOR category on a specific level. And return a list of doors' ids.

Input:

* Document doc: document of the revit model.
* ElementId levelid: the level you want to check.

Return:

A list of doors' elementIds.



（public IList<ElementId>） CheckDoor(Document doc)

Description:

Check all doors in the model document if they have required width and height.

Input:

* Document doc: document of the revit model.

Return:

A list of doors whose dimensions violate the Building Code.



(public Result) CeilingCheck(Document doc,ICollection<ElementId> roomids)

Description:

Check selected rooms' ceiling and the protruding objects from the ceiling if they have have enough headroom.

Input:

* Document doc: document of the revit model.
* ICollection<ElementId> roomids: selected rooms' ids.

Return:

The result of this check: success, cancel or fail.
