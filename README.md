# StaticCMS

A CMS written in F#, designed to manage and generate static websites.

## Motivation

The motivation behind StaticCMS was to create a tool to manage generating websites for code projects.

These websites would be static but would need to be updated reasonable often.

Rather than editing raw `html` it seemed better to be able to create content in `markdown` and generate the pages from
that (as well as other data sources).

There are multiple existing solutions for this but it seemed like a interesting project.

This is not meant to directly compete with them but to solve a problem I had.

## Philosophy

StaticCMS is designed with the following philosophy in mind:

* The end result should be readable/editable by humans.
* The process should be extendable.
* Data/content can come from multiple places and multiple formats. 

## Building a website

The build process is comprised of a pipeline of actions.

### Build configuration

In the sites project directory there is a `build.json` file.

This contains the build configuration for the site.

### Build actions

## Build actions

## Site file structure

Currently StaticCMS website projects have the following file structure:

* `$root/data` - a directory used to store data.
* `$root/fragment_templates` - Templated used in fragments
* `$root/page_templates` - Page templates
* `$root/pages` - Page data
* `$root/plugins` - Plugin data
* `$root/rendered` - The rendered site
* `$root/resources` - Any resources to used with the site
* `$root/build.json` - The build configuration

## Solution Structure

### StaticCMS

This is the core library which handles the basic functionality.

### StaticCMS.Actions

This is a collection of actions build on top of the core library to help simplify common tasks 
and share functionality between the app and web api.

It can also be seen as a reference library on how to perform common actions like initialize a site 
or add a page.

### StaticCMS.App

The CLI app project.

### StaticCMS.WebApi

The web api project.