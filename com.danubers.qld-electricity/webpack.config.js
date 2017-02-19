var path = require("path");

var config = {
    entry: ["./src/app.tsx"],
    devtool: "source-map",
    output: {
        path: path.resolve(__dirname, "wwwroot/dist"),
        filename: "bundle.js"
    },
    resolve: {
        extensions: [".ts", ".tsx", ".js"]
    },
    module: {
        loaders: [
            {
                test: /\.tsx?$/,
                loader: "ts-loader",
                exclude: '/node_modules/'
            }
        ]
    }
};
module.exports = config;

