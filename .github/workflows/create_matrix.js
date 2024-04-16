const tag_regex = /v(.*?)\-hk(.*)/;
const tag_prefix = "refs/tags/";

// @ts-check
/** @param {import("github-script").AsyncFunctionArguments} AsyncFunctionArguments */
module.exports = async ({ context }) => {
    const fs = require("node:fs/promises");

    const versions = JSON.parse(await fs.readFile("./versions.json", { encoding: "utf-8" }));
    
    // if the ref is a tag, limit the versions built to only the relevant version.
    if(context.ref.startsWith(tag_prefix)) {
        const tag_name = context.ref.slice(tag_prefix.length);
        const tag_hk_version = tag_regex.exec(tag_name)[2].trim();
        return versions.filter(x => x.version === tag_hk_version);
    } else {
        return versions;
    }
};