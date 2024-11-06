# IPMSpatialAnalysis
This is a project built for visualising and processing in-process monitoring (IPM) data collected from laser powder bed fusion (LPBF) machines. 

## Introduction
In-process monitoring creates huge datasets which are difficult to visualise and perform analysis. This project implements a voxel structure to organise and reduce the data, while calculating summary statistics and spatial correlations to identify significant areas for further analysis. 

## Use
The library was built with Grasshopper in mind and the important functionality has been abstracted into a suite of components for the canvas. At each step, the voxel fields can be previewed in Rhino using a colourmap to plot the scalar values of the voxels. This integration means that any meshes or CAD files (such as the original component, or CT scans) can be overlayed directly onto the IPM data. 

The class can be used without Rhino/Grasshopper if required. 
