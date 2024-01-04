# StaticCMS

A CMS written in F#, designed to manage and generate static websites.

## Motivation

The motivation behind StaticCMS was to create a tool to manage generating websites for code projects.

These websites would be static but would need to be updated reasonable often.

Rather than editing raw `html` it seemed better to be able to create content in `markdown` and generate the pages from
that (as well as other data sources).

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
* `$root/pages` - Page data.
